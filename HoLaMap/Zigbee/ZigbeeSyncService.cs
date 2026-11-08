using HoLaMap.Data;
using HoLaMap.Models;
using HoLaMap.Zigbee.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HoLaMap.Zigbee
{
    /// <summary>
    /// Copies Zigbee devices and their neighbour tables from ZHA into the database. Rooms are set by hand and left alone.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="client">The Home Assistant client.</param>
    /// <param name="options">The Home Assistant settings.</param>
    public class ZigbeeSyncService(HoLaMapContext context, HomeAssistantClient client, IOptions<HomeAssistantOptions> options)
    {
        /// <summary>
        /// Runs one sync.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the sync has been saved.</returns>
        public async Task SyncAsync(CancellationToken cancellationToken)
        {
            List<ZhaDevice> zhaDevices = await client.GetZhaDevicesAsync(cancellationToken);
            List<ZigbeeDevice> known = await context.ZigbeeDevices.Where(device => device.Network == ZigbeeNetwork.Zha).Include(device => device.Neighbors).ToListAsync(cancellationToken);
            HashSet<string> seen = [.. zhaDevices.Select(device => device.Ieee)];
            // A device that has left ZHA stays greyed out with its room in case it comes back, and is forgotten once it hasn't been heard from for a while.
            int forgetAfterDays = options.Value.ForgetAfterDays;
            foreach (ZigbeeDevice missing in known.Where(device => !seen.Contains(device.Ieee)))
            {
                if (forgetAfterDays > 0 && (missing.LastSeen == null || missing.LastSeen < DateTime.UtcNow.AddDays(-forgetAfterDays)))
                {
                    context.ZigbeeDevices.Remove(missing);
                    continue;
                }
                missing.Available = false;
                context.ZigbeeNeighbors.RemoveRange(missing.Neighbors);
                missing.Neighbors = [];
            }
            foreach (ZhaDevice zhaDevice in zhaDevices)
            {
                ZigbeeDevice? device = known.FirstOrDefault(item => item.Ieee == zhaDevice.Ieee);
                if (device == null)
                {
                    device = new ZigbeeDevice { Ieee = zhaDevice.Ieee };
                    context.ZigbeeDevices.Add(device);
                }
                device.Name = zhaDevice.UserGivenName ?? zhaDevice.Name ?? zhaDevice.Ieee;
                device.Manufacturer = zhaDevice.Manufacturer;
                device.Model = zhaDevice.Model;
                device.Role = zhaDevice.DeviceType ?? string.Empty;
                device.PowerSource = zhaDevice.PowerSource;
                device.Lqi = zhaDevice.Lqi;
                device.Rssi = zhaDevice.Rssi;
                device.LastSeen = DateTime.TryParse(zhaDevice.LastSeen, out DateTime lastSeen) ? lastSeen.ToUniversalTime() : device.LastSeen;
                device.Available = zhaDevice.Available;
                context.ZigbeeNeighbors.RemoveRange(device.Neighbors);
                device.Neighbors = [.. zhaDevice.Neighbors.Select(neighbor => new ZigbeeNeighbor { NeighborIeee = neighbor.Ieee, Lqi = neighbor.Lqi, Relationship = neighbor.Relationship })];
            }
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}