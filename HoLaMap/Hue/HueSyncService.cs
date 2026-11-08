using HoLaMap.Data;
using HoLaMap.Hue.Models;
using HoLaMap.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HoLaMap.Hue
{
    /// <summary>
    /// Copies the devices on the Hue Bridge into the database as a second Zigbee network. Rooms are set by hand and left alone.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="client">The Hue client.</param>
    /// <param name="options">The Hue settings.</param>
    public class HueSyncService(HoLaMapContext context, HueClient client, IOptions<HueOptions> options)
    {
        /// <summary>
        /// Runs one sync.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the sync has been saved.</returns>
        public async Task SyncAsync(CancellationToken cancellationToken)
        {
            string bridgeName = options.Value.BridgeHost;
            Device? bridge = await context.Devices.FirstOrDefaultAsync(device => device.Name == bridgeName, cancellationToken);
            if (string.IsNullOrEmpty(bridge?.Ip))
            {
                throw new InvalidOperationException($"No device called {bridgeName} with an IP address, so the Hue Bridge can't be reached.");
            }
            List<HueDevice> hueDevices = await client.GetDevicesAsync(bridge.Ip, cancellationToken);
            Dictionary<string, HueZigbeeConnectivity> radios = (await client.GetZigbeeConnectivityAsync(bridge.Ip, cancellationToken)).Where(radio => !string.IsNullOrEmpty(radio.MacAddress)).DistinctBy(radio => radio.Owner.Rid).ToDictionary(radio => radio.Owner.Rid);
            // The bridge is the network's coordinator and is already on the map as itself, so only the devices on its network are kept.
            List<HueDevice> onNetwork = [.. hueDevices.Where(device => radios.ContainsKey(device.Id) && device.Metadata.Archetype?.StartsWith("bridge", StringComparison.Ordinal) != true)];
            List<ZigbeeDevice> known = await context.ZigbeeDevices.Where(device => device.Network == ZigbeeNetwork.Hue).ToListAsync(cancellationToken);
            HashSet<string> seen = [.. onNetwork.Select(device => radios[device.Id].MacAddress!)];
            // A device that has left the bridge stays greyed out with its room in case it comes back, and is forgotten once it hasn't been connected for a while.
            int forgetAfterDays = options.Value.ForgetAfterDays;
            foreach (ZigbeeDevice missing in known.Where(device => !seen.Contains(device.Ieee)))
            {
                if (forgetAfterDays > 0 && (missing.LastSeen == null || missing.LastSeen < DateTime.UtcNow.AddDays(-forgetAfterDays)))
                {
                    context.ZigbeeDevices.Remove(missing);
                    continue;
                }
                missing.Available = false;
            }
            foreach (HueDevice hueDevice in onNetwork)
            {
                HueZigbeeConnectivity radio = radios[hueDevice.Id];
                ZigbeeDevice? device = known.FirstOrDefault(item => item.Ieee == radio.MacAddress);
                if (device == null)
                {
                    device = new ZigbeeDevice { Ieee = radio.MacAddress!, Network = ZigbeeNetwork.Hue };
                    context.ZigbeeDevices.Add(device);
                }
                // The API doesn't say which devices route, but anything on mains does and anything on a battery sleeps.
                bool battery = hueDevice.Services.Any(service => service.Rtype == "device_power");
                device.Name = hueDevice.Metadata.Name ?? hueDevice.ProductData.ProductName ?? radio.MacAddress!;
                device.Manufacturer = hueDevice.ProductData.ManufacturerName;
                device.Model = hueDevice.ProductData.ProductName;
                device.Role = battery ? "EndDevice" : "Router";
                device.PowerSource = battery ? "Battery" : "Mains";
                device.Available = radio.Status == "connected";
                device.LastSeen = device.Available ? DateTime.UtcNow : device.LastSeen;
            }
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}