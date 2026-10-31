using HoLaMap.Data;
using HoLaMap.Models;
using HoLaMap.Zigbee.Models;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Zigbee
{
    /// <summary>
    /// Copies Zigbee devices and their neighbour tables from ZHA into the database. Rooms are set by hand and left alone.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="client">The Home Assistant client.</param>
    public class ZigbeeSyncService(HoLaMapContext context, HomeAssistantClient client)
    {
        /// <summary>
        /// Runs one sync.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the sync has been saved.</returns>
        public async Task SyncAsync(CancellationToken cancellationToken)
        {
            List<ZhaDevice> zhaDevices = await client.GetZhaDevicesAsync(cancellationToken);
            List<ZigbeeDevice> known = await context.ZigbeeDevices.Include(device => device.Neighbors).ToListAsync(cancellationToken);
            HashSet<string> seen = [.. zhaDevices.Select(device => device.Ieee)];
            context.ZigbeeDevices.RemoveRange(known.Where(device => !seen.Contains(device.Ieee)));
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