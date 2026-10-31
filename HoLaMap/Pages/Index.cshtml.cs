using HoLaMap.Data;
using HoLaMap.Display;
using HoLaMap.Graph;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages
{
    /// <summary>
    /// Shows the physical network as an interactive map.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="configuration">The app configuration.</param>
    public class IndexModel(HoLaMapContext context, IConfiguration configuration) : PageModel
    {
        /// <summary>
        /// Handles GET requests for the page.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Turns the PoE splitter on a link on or off, from the map's details box.
        /// </summary>
        /// <param name="id">The link ID.</param>
        /// <returns>OK, or not found if the link has gone.</returns>
        public async Task<IActionResult> OnPostSplitterAsync(int id)
        {
            Link? link = await context.Links.FirstOrDefaultAsync(item => item.Id == id);
            if (link == null)
            {
                return NotFound();
            }
            link.PoESplitter = !link.PoESplitter;
            await context.SaveChangesAsync();
            return new OkResult();
        }

        /// <summary>
        /// Moves a device to another room, or out of every room, from the map's details box.
        /// </summary>
        /// <param name="id">The device ID.</param>
        /// <param name="roomId">The room, or null for no room.</param>
        /// <returns>OK, or not found if the device or room has gone.</returns>
        public async Task<IActionResult> OnPostRoomAsync(int id, int? roomId)
        {
            Device? device = await context.Devices.FirstOrDefaultAsync(item => item.Id == id);
            if (device == null || (roomId != null && !await context.Rooms.AnyAsync(room => room.Id == roomId)))
            {
                return NotFound();
            }
            device.RoomId = roomId;
            await context.SaveChangesAsync();
            return new OkResult();
        }

        /// <summary>
        /// Returns the devices and links to draw on the map.
        /// </summary>
        /// <returns>The graph as JSON.</returns>
        public async Task<JsonResult> OnGetGraphAsync()
        {
            List<Link> links = await context.Links.Include(link => link.FromPort).ThenInclude(port => port.Device).Include(link => link.ToDevice).Include(link => link.ToPort).ToListAsync();
            List<int> patchedIds = await context.PatchConnections.Where(connection => connection.DeviceId != null).Select(connection => connection.DeviceId!.Value).ToListAsync();
            HashSet<int> linkedIds = [.. links.Select(link => link.FromPort.DeviceId), .. links.Select(link => link.ToDeviceId), .. patchedIds];
            List<Device> devices = await context.Devices.Where(device => device.Kind != DeviceKind.Client || linkedIds.Contains(device.Id) || (device.WirelessApId != null && device.RoomId != null)).Include(device => device.Ports).Include(device => device.Room).ThenInclude(room => room!.Floor).Include(device => device.Rack).ToListAsync();
            Dictionary<int, int> upstreamPort = links.Where(link => link.FromPort.DeviceId != link.ToDeviceId).GroupBy(link => link.ToDeviceId).ToDictionary(group => group.Key, group => group.Min(link => link.FromPort.Index));
            devices = [.. devices.OrderBy(device => upstreamPort.GetValueOrDefault(device.Id)).ThenBy(device => device.Name)];
            links = [.. links.OrderBy(link => link.FromPort.Index).ThenBy(link => link.ToDevice.Name)];
            List<Floor> floors = await context.Floors.Include(floor => floor.Rooms).OrderBy(floor => floor.SortOrder).ThenBy(floor => floor.Name).ToListAsync();
            List<Rack> racks = await context.Racks.Include(rack => rack.Room).ThenInclude(room => room.Floor).Include(rack => rack.Devices).Include(rack => rack.Panels).ThenInclude(panel => panel.Connections).OrderBy(rack => rack.Name).ToListAsync();
            DateTime onlineSince = DateTime.UtcNow.AddMinutes(-configuration.GetValue<int>("Map:OfflineAfterMinutes"));
            HashSet<int> portDownIds = [.. links.Where(link => link.FromPort.DeviceId != link.ToDeviceId).GroupBy(link => link.ToDeviceId).Where(group => group.All(link => !link.FromPort.Up)).Select(group => group.Key)];
            // Wi-Fi clients only show when they've been given a room, so devices that stay put appear and phones and laptops don't.
            HashSet<int> shownIds = [.. devices.Select(device => device.Id)];
            List<Device> wireless = [.. devices.Where(device => device.WirelessApId != null && device.RoomId != null && shownIds.Contains(device.WirelessApId.Value)).OrderBy(device => device.Name)];
            List<Network> networks = await context.Networks.OrderBy(network => network.VlanId ?? 0).ThenBy(network => network.Name).ToListAsync();
            GraphData graph = new()
            {
                Nodes = [.. devices.Select(device => new GraphNode { Id = $"d{device.Id}", Label = device.Name, Kind = device.Kind.ToString().ToLowerInvariant(), Detail = string.Join(" · ", new[] { device.Model, device.Ip, device.Room == null ? null : $"{device.Room.Floor.Name} / {device.Room.Name}", device.Rack == null ? null : $"{device.Rack.Name} U{device.RackU}", device.Mac, $"Last Seen {device.LastSeen.ToLocalTime():d MMM HH:mm}" }.Where(part => !string.IsNullOrEmpty(part))), Room = device.RoomId == null ? string.Empty : $"r{device.RoomId}", Rack = device.RackId == null ? string.Empty : $"k{device.RackId}", Ip = device.Ip ?? string.Empty, Model = device.Model ?? string.Empty, Mac = device.Mac, LastSeen = device.LastSeen.ToLocalTime().ToString("d MMM HH:mm"), RackU = device.RackU ?? 0, RackColumn = device.RackColumn ?? 0, RackWidth = device.RackWidth, RackHeight = device.RackHeight, Online = device.LastSeen >= onlineSince && !portDownIds.Contains(device.Id), Ports = [.. device.Ports.OrderBy(port => port.Index).Select(port => new GraphPort { Id = $"p{port.Id}", Index = port.Index, Style = PortStyle.Class(port), Detail = PortStyle.Tooltip(port), Network = port.NetworkName ?? string.Empty, Tagged = port.TaggedNetworks, Name = port.Name ?? string.Empty, Up = port.Up, Speed = port.Speed, PoECapable = port.PoE, PoEPower = port.PoEPower })] })],
                Edges = [.. links.Select(link => new GraphEdge { Id = $"l{link.Id}", Source = $"d{link.FromPort.DeviceId}", Target = $"d{link.ToDeviceId}", SourcePort = $"p{link.FromPortId}", TargetPort = link.ToPortId != null ? $"p{link.ToPortId}" : string.Empty, Label = link.ToPort == null ? $"{link.FromPort.Index}" : $"{link.FromPort.Index} → {link.ToPort.Index}", Style = PortStyle.Class(link.FromPort), Manual = link.Source == LinkSource.Manual, PoE = PortStyle.IsPoE(link.FromPort), Splitter = link.PoESplitter, Notes = link.Notes ?? string.Empty, Online = link.ToDevice.LastSeen >= onlineSince && (link.FromPort.Up || link.FromPort.DeviceId == link.ToDeviceId), Detail = string.Join(" · ", new[] { PortStyle.Tooltip(link.FromPort), link.PoESplitter ? "PoE Splitter" : null, link.Notes }.Where(part => !string.IsNullOrEmpty(part))) }), .. wireless.Select(device => new GraphEdge { Id = $"w{device.Id}", Source = $"d{device.WirelessApId}", Target = $"d{device.Id}", Label = device.WifiBand ?? "Wi-Fi", Style = "wifi", Wireless = true, Ssid = device.Ssid ?? string.Empty, Band = device.WifiBand ?? string.Empty, Channel = device.WifiChannel, Signal = device.WifiSignal, Rate = device.WifiRate, Online = device.LastSeen >= onlineSince, Detail = string.Join(" · ", new[] { device.Ssid, device.WifiBand, device.WifiChannel == null ? null : $"Channel {device.WifiChannel}", device.WifiSignal == null ? null : $"{device.WifiSignal} dBm", device.WifiRate == null ? null : $"{device.WifiRate} Mbps" }.Where(part => !string.IsNullOrEmpty(part))) })],
                Floors = [.. floors.Where(floor => floor.Rooms.Count > 0).Select(floor => new GraphFloor { Id = $"f{floor.Id}", Name = floor.Name, Rooms = [.. floor.Rooms.OrderBy(room => room.Name).Select(room => new GraphRoom { Id = $"r{room.Id}", Name = room.Name, X = room.X, Y = room.Y, Width = room.Width, Height = room.Height })] })],
                Racks = [.. racks.Select(rack => new GraphRack { Id = $"k{rack.Id}", Name = rack.Name, Room = $"r{rack.RoomId}", HeightU = rack.HeightU, Panels = [.. rack.Panels.Select(panel => new GraphPanel { Id = $"q{panel.Id}", Kind = panel.Kind.ToString().ToLowerInvariant(), Name = panel.Name ?? $"{panel.Kind} Panel", Ports = panel.PortCount, Patches = [.. panel.Connections.Select(connection => new GraphPatch { Number = connection.Number, Side = connection.Side.ToString().ToLowerInvariant(), Node = connection.DeviceId == null ? string.Empty : $"d{connection.DeviceId}", Port = connection.PortId == null ? string.Empty : $"p{connection.PortId}", Panel = connection.TargetPanelId == null ? string.Empty : $"q{connection.TargetPanelId}", TargetNumber = connection.TargetNumber ?? 0, TargetSide = connection.TargetSide?.ToString().ToLowerInvariant() ?? string.Empty })], RackU = panel.RackU, RackColumn = panel.RackColumn, RackWidth = panel.RackWidth, RackHeight = panel.RackHeight })], Detail = $"{rack.Room.Floor.Name} / {rack.Room.Name} · {rack.HeightU}U · {(rack.Devices.Count == 0 ? "Empty" : string.Join(", ", rack.Devices.OrderByDescending(device => device.RackU).Select(device => device.Name)))}", Online = rack.Devices.Count == 0 || rack.Devices.Any(device => device.LastSeen >= onlineSince && !portDownIds.Contains(device.Id)) })],
                Networks = [.. networks.Select(network => new GraphNetwork { Name = network.Name, VlanId = network.VlanId, Subnet = network.Subnet ?? string.Empty })]
            };
            return new JsonResult(graph);
        }
    }
}