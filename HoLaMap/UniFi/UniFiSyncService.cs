using HoLaMap.Data;
using HoLaMap.Models;
using HoLaMap.UniFi.Models;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.UniFi
{
    /// <summary>
    /// Syncs devices, ports, links and networks from UniFi into the database.
    /// </summary>
    /// <param name="client">The UniFi API client.</param>
    /// <param name="context">The database context.</param>
    public class UniFiSyncService(UniFiClient client, HoLaMapContext context)
    {
        /// <summary>
        /// Runs a full sync.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the sync has finished.</returns>
        public async Task SyncAsync(CancellationToken cancellationToken)
        {
            List<UniFiDevice> unifiDevices = await client.GetDevicesAsync(cancellationToken);
            List<UniFiStation> stations = await client.GetActiveClientsAsync(cancellationToken);
            List<UniFiNetwork> networks = await client.GetNetworksAsync(cancellationToken);
            List<UniFiPortProfile> profiles = await client.GetPortProfilesAsync(cancellationToken);
            DateTime now = DateTime.UtcNow;
            await SyncNetworksAsync(networks, cancellationToken);

            // Gateways, switches and access points.
            List<Device> known = await context.Devices.Include(device => device.Ports).Include(device => device.Macs).ToListAsync(cancellationToken);
            Dictionary<string, Device> devices = known.ToDictionary(device => device.Mac);
            foreach (Device device in known)
            {
                device.Macs.ForEach(deviceMac => devices[deviceMac.Mac] = device);
            }
            HashSet<string> unifiMacs = [];
            NetworkResolver resolver = new(networks, profiles);
            foreach (UniFiDevice unifiDevice in unifiDevices)
            {
                Device device = GetOrAdd(devices, unifiDevice.Mac);
                unifiMacs.Add(device.Mac);
                device.Name = unifiDevice.Name ?? device.Mac;
                device.Kind = KindFromType(unifiDevice.Type);
                device.Model = unifiDevice.Model;
                device.Ip = unifiDevice.Ip;
                device.LastSeen = now;
                SyncPorts(device, unifiDevice, resolver);
            }

            // Connected clients.
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
            await context.SaveChangesAsync(cancellationToken);
            await SyncLinksAsync(devices, unifiDevices, stations, now, cancellationToken);
        }

        /// <summary>
        /// Upserts networks by name.
        /// </summary>
        /// <param name="networks">Networks from UniFi.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the networks have been saved.</returns>
        private async Task SyncNetworksAsync(List<UniFiNetwork> networks, CancellationToken cancellationToken)
        {
            Dictionary<string, Network> existing = await context.Networks.ToDictionaryAsync(network => network.Name, cancellationToken);
            foreach (UniFiNetwork unifiNetwork in networks.Where(network => !string.IsNullOrEmpty(network.Name) && !string.IsNullOrEmpty(network.IpSubnet)))
            {
                if (!existing.TryGetValue(unifiNetwork.Name!, out Network? network))
                {
                    network = new Network { Name = unifiNetwork.Name! };
                    context.Networks.Add(network);
                }
                network.VlanId = unifiNetwork.Vlan;
                network.Subnet = unifiNetwork.IpSubnet;
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Updates a device's ports from a UniFi port table, removing ports that no longer exist.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="unifiDevice">The UniFi device and its port table.</param>
        /// <param name="resolver">Resolves each port's native network.</param>
        private void SyncPorts(Device device, UniFiDevice unifiDevice, NetworkResolver resolver)
        {
            List<UniFiPort> portTable = unifiDevice.PortTable;
            HashSet<int> indexes = [.. portTable.Select(unifiPort => unifiPort.PortIdx)];
            foreach (Port removed in device.Ports.Where(port => !indexes.Contains(port.Index)).ToList())
            {
                context.Ports.Remove(removed);
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
                port.PoEPower = unifiPort.Up && unifiPort.PoePower > 0 ? unifiPort.PoePower : null;
                port.Up = unifiPort.Up;
                port.IsUplink = unifiPort.IsUplink;
                port.NetworkName = resolver.Resolve(unifiPort, unifiDevice.PortOverrides.FirstOrDefault(portOverride => portOverride.PortIdx == unifiPort.PortIdx));
            }
        }

        /// <summary>
        /// Records links from switch uplinks and wired clients, adding to free ports and raising action items for collisions.
        /// </summary>
        /// <param name="devices">All devices keyed by MAC.</param>
        /// <param name="unifiDevices">UniFi devices.</param>
        /// <param name="stations">Connected clients.</param>
        /// <param name="now">The time of this sync (UTC).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the links have been saved.</returns>
        private async Task SyncLinksAsync(Dictionary<string, Device> devices, List<UniFiDevice> unifiDevices, List<UniFiStation> stations, DateTime now, CancellationToken cancellationToken)
        {
            List<Link> links = await context.Links.ToListAsync(cancellationToken);
            List<ActionItem> actions = await context.ActionItems.ToListAsync(cancellationToken);

            // Switch and AP uplinks are trusted, so they always replace whatever UniFi reported before.
            foreach (UniFiDevice unifiDevice in unifiDevices.Where(unifiDevice => unifiDevice.Uplink?.UplinkMac != null && unifiDevice.Uplink.UplinkRemotePort != null))
            {
                Port? fromPort = FindPort(devices, unifiDevice.Uplink!.UplinkMac!, unifiDevice.Uplink.UplinkRemotePort!.Value);
                Device toDevice = devices[NormaliseMac(unifiDevice.Mac)];
                if (fromPort == null)
                {
                    continue;
                }
                foreach (Link stale in links.Where(link => link.Source == LinkSource.UniFi && (link.ToDeviceId == toDevice.Id || link.FromPortId == fromPort.Id) && !(link.FromPortId == fromPort.Id && link.ToDeviceId == toDevice.Id)).ToList())
                {
                    RemoveLink(links, stale);
                }
                Link link = links.FirstOrDefault(existing => existing.FromPortId == fromPort.Id && existing.ToDeviceId == toDevice.Id) ?? AddLink(links, fromPort, toDevice);
                link.ToPortId = (toDevice.Ports.FirstOrDefault(port => port.Index == unifiDevice.Uplink.PortIdx) ?? toDevice.Ports.FirstOrDefault(port => port.IsUplink))?.Id;
                link.LastSeen = now;
                RemoveActions(actions, toDevice.Id);
            }

            // A client can't really be on a switch's uplink port, or on a port feeding another switch or AP; UniFi reports these when it only sees the MAC through that cable.
            HashSet<int> infrastructureIds = [.. devices.Values.Where(device => device.Kind != DeviceKind.Client).Select(device => device.Id)];
            HashSet<int> throughPortIds = [.. devices.Values.SelectMany(device => device.Ports).Where(port => port.IsUplink).Select(port => port.Id), .. links.Where(link => infrastructureIds.Contains(link.ToDeviceId)).Select(link => link.FromPortId)];
            foreach (Link upstream in links.Where(link => link.Source == LinkSource.UniFi && !infrastructureIds.Contains(link.ToDeviceId) && throughPortIds.Contains(link.FromPortId)).ToList())
            {
                RemoveLink(links, upstream);
            }

            // Devices with a manual link stay where they were put.
            HashSet<int> manualIds = [.. links.Where(link => link.Source == LinkSource.Manual).Select(link => link.ToDeviceId)];
            foreach (Link overridden in links.Where(link => link.Source == LinkSource.UniFi && manualIds.Contains(link.ToDeviceId) && !infrastructureIds.Contains(link.ToDeviceId)).ToList())
            {
                RemoveLink(links, overridden);
            }

            // Wired clients: free or shared ports are assigned automatically, anything else becomes an action item.
            foreach (UniFiStation station in stations.Where(station => station.IsWired && station.SwMac != null && station.SwPort != null))
            {
                Port? port = FindPort(devices, station.SwMac!, station.SwPort!.Value);
                if (port == null || throughPortIds.Contains(port.Id) || !devices.TryGetValue(NormaliseMac(station.Mac), out Device? device) || manualIds.Contains(device.Id))
                {
                    continue;
                }
                Link? existing = links.FirstOrDefault(link => link.FromPortId == port.Id && link.ToDeviceId == device.Id);
                if (existing != null)
                {
                    existing.LastSeen = now;
                    RemoveActions(actions, device.Id);
                    continue;
                }
                if (!port.Shared && links.Any(link => link.FromPortId == port.Id))
                {
                    ActionItem? action = actions.FirstOrDefault(item => item.PortId == port.Id && item.DeviceId == device.Id);
                    if (action == null)
                    {
                        action = new ActionItem { PortId = port.Id, DeviceId = device.Id, FirstSeen = now };
                        actions.Add(action);
                        context.ActionItems.Add(action);
                    }
                    action.LastSeen = now;
                    continue;
                }
                foreach (Link moved in links.Where(link => link.ToDeviceId == device.Id && link.Source == LinkSource.UniFi).ToList())
                {
                    RemoveLink(links, moved);
                }
                AddLink(links, port, device).LastSeen = now;
                RemoveActions(actions, device.Id);
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Adds a UniFi link and tracks it in the working list.
        /// </summary>
        /// <param name="links">The working list of links.</param>
        /// <param name="port">The port the link starts from.</param>
        /// <param name="device">The device the link goes to.</param>
        /// <returns>The new link.</returns>
        private Link AddLink(List<Link> links, Port port, Device device)
        {
            Link link = new Link { FromPortId = port.Id, ToDeviceId = device.Id, Source = LinkSource.UniFi };
            links.Add(link);
            context.Links.Add(link);
            return link;
        }

        /// <summary>
        /// Removes a link and stops tracking it in the working list.
        /// </summary>
        /// <param name="links">The working list of links.</param>
        /// <param name="link">The link to remove.</param>
        private void RemoveLink(List<Link> links, Link link)
        {
            links.Remove(link);
            context.Links.Remove(link);
        }

        /// <summary>
        /// Removes any action items for a device, because it has now been seen on an assigned port.
        /// </summary>
        /// <param name="actions">The working list of action items.</param>
        /// <param name="deviceId">The device ID.</param>
        private void RemoveActions(List<ActionItem> actions, int deviceId)
        {
            foreach (ActionItem action in actions.Where(item => item.DeviceId == deviceId).ToList())
            {
                actions.Remove(action);
                context.ActionItems.Remove(action);
            }
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
                context.Devices.Add(device);
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