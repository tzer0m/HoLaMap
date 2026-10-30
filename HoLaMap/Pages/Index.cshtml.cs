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
        /// Returns the devices and links to draw on the map.
        /// </summary>
        /// <returns>The graph as JSON.</returns>
        public async Task<JsonResult> OnGetGraphAsync()
        {
            List<Link> links = await context.Links.Include(link => link.FromPort).ThenInclude(port => port.Device).Include(link => link.ToDevice).Include(link => link.ToPort).ToListAsync();
            HashSet<int> linkedIds = [.. links.Select(link => link.FromPort.DeviceId), .. links.Select(link => link.ToDeviceId)];
            List<Device> devices = await context.Devices.Where(device => device.Kind != DeviceKind.Client || linkedIds.Contains(device.Id)).Include(device => device.Ports).Include(device => device.Room).ThenInclude(room => room!.Floor).Include(device => device.Rack).ToListAsync();
            Dictionary<int, int> upstreamPort = links.Where(link => link.FromPort.DeviceId != link.ToDeviceId).GroupBy(link => link.ToDeviceId).ToDictionary(group => group.Key, group => group.Min(link => link.FromPort.Index));
            devices = [.. devices.OrderBy(device => upstreamPort.GetValueOrDefault(device.Id)).ThenBy(device => device.Name)];
            links = [.. links.OrderBy(link => link.FromPort.Index).ThenBy(link => link.ToDevice.Name)];
            List<Floor> floors = await context.Floors.Include(floor => floor.Rooms).OrderBy(floor => floor.SortOrder).ThenBy(floor => floor.Name).ToListAsync();
            List<Rack> racks = await context.Racks.Include(rack => rack.Room).ThenInclude(room => room.Floor).Include(rack => rack.Devices).Include(rack => rack.Panels).OrderBy(rack => rack.Name).ToListAsync();
            DateTime onlineSince = DateTime.UtcNow.AddMinutes(-configuration.GetValue<int>("Map:OfflineAfterMinutes"));
            HashSet<int> portDownIds = [.. links.Where(link => link.FromPort.DeviceId != link.ToDeviceId).GroupBy(link => link.ToDeviceId).Where(group => group.All(link => !link.FromPort.Up)).Select(group => group.Key)];
            GraphData graph = new()
            {
                Nodes = [.. devices.Select(device => new GraphNode { Id = $"d{device.Id}", Label = device.Name, Kind = device.Kind.ToString().ToLowerInvariant(), Detail = string.Join(" · ", new[] { device.Model, device.Ip, device.Room == null ? null : $"{device.Room.Floor.Name} / {device.Room.Name}", device.Rack == null ? null : $"{device.Rack.Name} U{device.RackU}", device.Mac, $"Last Seen {device.LastSeen.ToLocalTime():d MMM HH:mm}" }.Where(part => !string.IsNullOrEmpty(part))), Room = device.RoomId == null ? string.Empty : $"r{device.RoomId}", Rack = device.RackId == null ? string.Empty : $"k{device.RackId}", RackU = device.RackU ?? 0, RackColumn = device.RackColumn ?? 0, RackWidth = device.RackWidth, RackHeight = device.RackHeight, Online = device.LastSeen >= onlineSince && !portDownIds.Contains(device.Id), Ports = [.. device.Ports.OrderBy(port => port.Index).Select(port => new GraphPort { Id = $"p{port.Id}", Index = port.Index, Style = PortStyle.Class(port), Detail = PortStyle.Tooltip(port) })] })],
                Edges = [.. links.Select(link => new GraphEdge { Id = $"l{link.Id}", Source = $"d{link.FromPort.DeviceId}", Target = $"d{link.ToDeviceId}", SourcePort = $"p{link.FromPortId}", TargetPort = link.ToPortId != null ? $"p{link.ToPortId}" : string.Empty, Label = link.ToPort == null ? $"{link.FromPort.Index}" : $"{link.FromPort.Index} → {link.ToPort.Index}", Style = PortStyle.Class(link.FromPort), Manual = link.Source == LinkSource.Manual, PoE = PortStyle.IsPoE(link.FromPort), Online = link.ToDevice.LastSeen >= onlineSince && (link.FromPort.Up || link.FromPort.DeviceId == link.ToDeviceId), Detail = string.IsNullOrEmpty(link.Notes) ? PortStyle.Tooltip(link.FromPort) : $"{PortStyle.Tooltip(link.FromPort)} · {link.Notes}" })],
                Floors = [.. floors.Where(floor => floor.Rooms.Count > 0).Select(floor => new GraphFloor { Id = $"f{floor.Id}", Name = floor.Name, Rooms = [.. floor.Rooms.OrderBy(room => room.Name).Select(room => new GraphRoom { Id = $"r{room.Id}", Name = room.Name, X = room.X, Y = room.Y, Width = room.Width, Height = room.Height })] })],
                Racks = [.. racks.Select(rack => new GraphRack { Id = $"k{rack.Id}", Name = rack.Name, Room = $"r{rack.RoomId}", HeightU = rack.HeightU, Panels = [.. rack.Panels.Select(panel => new GraphPanel { Kind = panel.Kind.ToString().ToLowerInvariant(), RackU = panel.RackU, RackColumn = panel.RackColumn, RackWidth = panel.RackWidth, RackHeight = panel.RackHeight })], Detail = $"{rack.Room.Floor.Name} / {rack.Room.Name} · {rack.HeightU}U · {(rack.Devices.Count == 0 ? "Empty" : string.Join(", ", rack.Devices.OrderByDescending(device => device.RackU).Select(device => device.Name)))}", Online = rack.Devices.Count == 0 || rack.Devices.Any(device => device.LastSeen >= onlineSince && !portDownIds.Contains(device.Id)) })]
            };
            return new JsonResult(graph);
        }
    }
}