using HoLaMap.Data;
using HoLaMap.Models;
using HoLaMap.UniFi.Models;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.UniFi
{
    /// <summary>
    /// Syncs devices, ports, links and networks from UniFi into the database.
    /// </summary>
    public class UniFiSyncService
    {
        /// <summary>
        /// The UniFi API client.
        /// </summary>
        private readonly UniFiClient Client;

        /// <summary>
        /// The database context.
        /// </summary>
        private readonly HoLaMapContext Context;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniFiSyncService"/> class.
        /// </summary>
        /// <param name="client">The UniFi API client.</param>
        /// <param name="context">The database context.</param>
        public UniFiSyncService(UniFiClient client, HoLaMapContext context)
        {
            Client = client;
            Context = context;
        }

        /// <summary>
        /// Runs a full sync.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the sync has finished.</returns>
        public async Task SyncAsync(CancellationToken cancellationToken)
        {
            List<UniFiDevice> unifiDevices = await Client.GetDevicesAsync(cancellationToken);
            List<UniFiStation> stations = await Client.GetActiveClientsAsync(cancellationToken);
            List<UniFiNetwork> networks = await Client.GetNetworksAsync(cancellationToken);
            DateTime now = DateTime.UtcNow;

            await SyncNetworksAsync(networks, cancellationToken);

            Dictionary<string, Device> devices = await Context.Devices.Include(device => device.Ports).ToDictionaryAsync(device => device.Mac, cancellationToken);
            HashSet<string> unifiMacs = new HashSet<string>();
            foreach (UniFiDevice unifiDevice in unifiDevices)
            {
                Device device = GetOrAdd(devices, unifiDevice.Mac);
                unifiMacs.Add(device.Mac);
                device.Name = unifiDevice.Name ?? device.Mac;
                device.Kind = KindFromType(unifiDevice.Type);
                device.Model = unifiDevice.Model;
                device.Ip = unifiDevice.Ip;
                device.LastSeen = now;
                SyncPorts(device, unifiDevice.PortTable);
            }

            foreach (UniFiStation station in stations)
            {
                Device device = GetOrAdd(devices, station.Mac);
                if (unifiMacs.Contains(device.Mac))
                {
                    continue;
                }

                device.Name = station.Name ?? station.Hostname ?? device.Mac;
                device.Kind = DeviceKind.Client;
                device.Ip = station.Ip;
                device.LastSeen = now;
            }

            await Context.SaveChangesAsync(cancellationToken);
            await SyncLinksAsync(devices, unifiDevices, stations, cancellationToken);
        }

        /// <summary>
        /// Upserts networks by name.
        /// </summary>
        /// <param name="networks">Networks from UniFi.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the networks have been saved.</returns>
        private async Task SyncNetworksAsync(List<UniFiNetwork> networks, CancellationToken cancellationToken)
        {
            Dictionary<string, Network> existing = await Context.Networks.ToDictionaryAsync(network => network.Name, cancellationToken);
            foreach (UniFiNetwork unifiNetwork in networks.Where(network => !string.IsNullOrEmpty(network.Name) && !string.IsNullOrEmpty(network.IpSubnet)))
            {
                if (!existing.TryGetValue(unifiNetwork.Name!, out Network? network))
                {
                    network = new Network { Name = unifiNetwork.Name! };
                    Context.Networks.Add(network);
                }

                network.VlanId = unifiNetwork.Vlan;
                network.Subnet = unifiNetwork.IpSubnet;
            }

            await Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Updates a device's ports from a UniFi port table, removing ports that no longer exist.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="portTable">The UniFi port table.</param>
        private void SyncPorts(Device device, List<UniFiPort> portTable)
        {
            HashSet<int> indexes = portTable.Select(unifiPort => unifiPort.PortIdx).ToHashSet();
            foreach (Port removed in device.Ports.Where(port => !indexes.Contains(port.Index)).ToList())
            {
                Context.Ports.Remove(removed);
            }

            foreach (UniFiPort unifiPort in portTable)
            {
                Port? port = device.Ports.FirstOrDefault(existing => existing.Index == unifiPort.PortIdx);
                if (port == null)
                {
                    port = new Port { Index = unifiPort.PortIdx };
                    device.Ports.Add(port);
                }

                port.Name = unifiPort.Name;
                port.MaxSpeed = MaxSpeedFromMedia(unifiPort.Media);
                port.Speed = unifiPort.Up ? unifiPort.Speed : null;
                port.PoE = unifiPort.PortPoe;
                port.Up = unifiPort.Up;
                port.IsUplink = unifiPort.IsUplink;
                port.NetworkName = unifiPort.NetworkName;
            }
        }

        /// <summary>
        /// Rebuilds all links from switch uplinks and wired client switch ports.
        /// </summary>
        /// <param name="devices">All devices keyed by MAC.</param>
        /// <param name="unifiDevices">UniFi devices.</param>
        /// <param name="stations">Connected clients.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the links have been saved.</returns>
        private async Task SyncLinksAsync(Dictionary<string, Device> devices, List<UniFiDevice> unifiDevices, List<UniFiStation> stations, CancellationToken cancellationToken)
        {
            Context.Links.RemoveRange(await Context.Links.ToListAsync(cancellationToken));

            foreach (UniFiDevice unifiDevice in unifiDevices.Where(unifiDevice => unifiDevice.Uplink?.UplinkMac != null && unifiDevice.Uplink.UplinkRemotePort != null))
            {
                Port? fromPort = FindPort(devices, unifiDevice.Uplink!.UplinkMac!, unifiDevice.Uplink.UplinkRemotePort!.Value);
                Device toDevice = devices[NormaliseMac(unifiDevice.Mac)];
                if (fromPort == null)
                {
                    continue;
                }

                Port? toPort = toDevice.Ports.FirstOrDefault(port => port.Index == unifiDevice.Uplink.PortIdx) ?? toDevice.Ports.FirstOrDefault(port => port.IsUplink);
                Context.Links.Add(new Link { FromPort = fromPort, ToDevice = toDevice, ToPort = toPort });
            }

            foreach (UniFiStation station in stations.Where(station => station.IsWired && station.SwMac != null && station.SwPort != null))
            {
                Port? fromPort = FindPort(devices, station.SwMac!, station.SwPort!.Value);
                if (fromPort == null || !devices.TryGetValue(NormaliseMac(station.Mac), out Device? toDevice))
                {
                    continue;
                }

                Context.Links.Add(new Link { FromPort = fromPort, ToDevice = toDevice });
            }

            await Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Finds a port on a device by the device's MAC and the port number.
        /// </summary>
        /// <param name="devices">All devices keyed by MAC.</param>
        /// <param name="mac">The device MAC.</param>
        /// <param name="index">The port number.</param>
        /// <returns>The port, or null if not found.</returns>
        private static Port? FindPort(Dictionary<string, Device> devices, string mac, int index)
        {
            return devices.TryGetValue(NormaliseMac(mac), out Device? device) ? device.Ports.FirstOrDefault(port => port.Index == index) : null;
        }

        /// <summary>
        /// Gets an existing device by MAC or adds a new one.
        /// </summary>
        /// <param name="devices">All devices keyed by MAC.</param>
        /// <param name="mac">The MAC address.</param>
        /// <returns>The device.</returns>
        private Device GetOrAdd(Dictionary<string, Device> devices, string mac)
        {
            string normalised = NormaliseMac(mac);
            if (!devices.TryGetValue(normalised, out Device? device))
            {
                device = new Device { Mac = normalised };
                devices.Add(normalised, device);
                Context.Devices.Add(device);
            }

            return device;
        }

        /// <summary>
        /// Normalises a MAC address to lowercase colon format.
        /// </summary>
        /// <param name="mac">The MAC address.</param>
        /// <returns>The normalised MAC address.</returns>
        private static string NormaliseMac(string mac) => mac.Trim().Replace('-', ':').ToLowerInvariant();

        /// <summary>
        /// Maps a UniFi device type to a device kind.
        /// </summary>
        /// <param name="type">The UniFi device type.</param>
        /// <returns>The device kind.</returns>
        private static DeviceKind KindFromType(string? type) => type switch { "udm" or "ugw" or "uxg" => DeviceKind.Gateway, "usw" => DeviceKind.Switch, "uap" => DeviceKind.AccessPoint, _ => DeviceKind.Client };

        /// <summary>
        /// Maps a UniFi port media type to its maximum speed.
        /// </summary>
        /// <param name="media">The media type.</param>
        /// <returns>The maximum speed in Mbps, or null if unknown.</returns>
        private static int? MaxSpeedFromMedia(string? media) => media switch { "FE" => 100, "GE" => 1000, "2P5GE" => 2500, "SFP" => 1000, "SFP+" => 10000, "10GE" => 10000, _ => null };
    }
}