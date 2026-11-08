using HoLaMap.Data;
using HoLaMap.Display;
using HoLaMap.Graph;
using HoLaMap.Models;
using HoLaMap.Zigbee;
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
        /// Returns the Zigbee mesh as a tree: routers back to the coordinator along their strongest links, end devices under their parents, and the coordinator under the device it's plugged into.
        /// </summary>
        /// <returns>The Zigbee devices and links as JSON.</returns>
        public async Task<JsonResult> OnGetZigbeeAsync()
        {
            List<ZigbeeDevice> devices = await context.ZigbeeDevices.Include(device => device.Neighbors).OrderBy(device => device.Name).ToListAsync();
            Dictionary<int, (ZigbeeDevice Parent, int? Lqi)> parents = ZigbeeMesh.Parents([.. devices.Where(device => device.Network == ZigbeeNetwork.Zha)]);
            string hostName = configuration["HomeAssistant:CoordinatorHost"] ?? string.Empty;
            Device? host = hostName.Length == 0 ? null : await context.Devices.FirstOrDefaultAsync(device => device.Name == hostName);
            ZigbeeDevice? coordinator = devices.FirstOrDefault(device => device.Network == ZigbeeNetwork.Zha && device.Role == "Coordinator");
            DateTime onlineSince = DateTime.UtcNow.AddMinutes(-configuration.GetValue<int>("Map:OfflineAfterMinutes"));
            GraphData graph = new()
            {
                Nodes = [.. devices.Select(device => new GraphNode { Id = $"z{device.Id}", Label = device.Name, Kind = device.Role switch { "Coordinator" => "coordinator", "Router" => "router", _ => "client" }, Online = device.Available, Room = device.RoomId == null ? string.Empty : $"r{device.RoomId}", Model = device.Model ?? string.Empty, Mac = device.Ieee, Manufacturer = device.Manufacturer ?? string.Empty, Power = device.PowerSource ?? string.Empty, Lqi = device.Lqi, Rssi = device.Rssi, Hue = device.Network == ZigbeeNetwork.Hue, LastSeen = device.LastSeen == null ? string.Empty : Time(device.LastSeen.Value) })],
                Edges = [.. parents.Select(pair => new GraphEdge { Id = $"zl{pair.Key}", Source = $"z{pair.Value.Parent.Id}", Target = $"z{pair.Key}", Label = pair.Value.Lqi == null ? "Zigbee" : $"LQI {pair.Value.Lqi}", Style = pair.Value.Lqi >= 150 ? "port-1000" : pair.Value.Lqi >= 80 ? "port-100" : "port-10", Zigbee = true, Lqi = pair.Value.Lqi, Online = devices.First(device => device.Id == pair.Key).Available })]
            };
            if (host != null && coordinator != null)
            {
                graph.Nodes.Insert(0, ToNode(host, "client", host.LastSeen >= onlineSince));
                graph.Edges.Insert(0, new GraphEdge { Id = "zl-usb", Source = $"d{host.Id}", Target = $"z{coordinator.Id}", Label = "USB", Style = "port-down", Online = true, Detail = "Zigbee coordinator plugged into the host" });
            }
            string bridgeName = configuration["Hue:BridgeHost"] ?? string.Empty;
            Device? bridge = bridgeName.Length == 0 ? null : await context.Devices.FirstOrDefaultAsync(device => device.Name == bridgeName);
            List<ZigbeeDevice> hueDevices = [.. devices.Where(device => device.Network == ZigbeeNetwork.Hue)];
            if (bridge != null && hueDevices.Count > 0)
            {
                // The Hue Bridge is the coordinator of its own network. Its API doesn't share the mesh, so every device hangs straight off it.
                graph.Nodes.Add(ToNode(bridge, "coordinator", bridge.LastSeen >= onlineSince));
                graph.Edges.AddRange(hueDevices.Select(device => new GraphEdge { Id = $"zl{device.Id}", Source = $"d{bridge.Id}", Target = $"z{device.Id}", Label = "Zigbee", Style = "port-1000", Zigbee = true, Online = device.Available, Detail = "On the Hue Bridge's network" }));
            }
            return new JsonResult(graph);
        }

        /// <summary>
        /// Moves a Zigbee device to another room, or out of every room, from the map's details box.
        /// </summary>
        /// <param name="id">The Zigbee device ID.</param>
        /// <param name="roomId">The room, or null for no room.</param>
        /// <returns>OK, or not found if the device or room has gone.</returns>
        public async Task<IActionResult> OnPostZigbeeRoomAsync(int id, int? roomId)
        {
            ZigbeeDevice? device = await context.ZigbeeDevices.FirstOrDefaultAsync(item => item.Id == id);
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
                Nodes = [.. devices.Select(device => ToMapNode(device, device.LastSeen >= onlineSince && !portDownIds.Contains(device.Id)))],
                Edges = [.. links.Select(link => ToEdge(link, onlineSince)), .. wireless.Select(device => ToWirelessEdge(device, onlineSince))],
                Floors = [.. floors.Where(floor => floor.Rooms.Count > 0).Select(floor => new GraphFloor { Id = $"f{floor.Id}", Name = floor.Name, Rooms = [.. floor.Rooms.OrderBy(room => room.Name).Select(room => new GraphRoom { Id = $"r{room.Id}", Name = room.Name, X = room.X, Y = room.Y, Width = room.Width, Height = room.Height })] })],
                Racks = [.. racks.Select(rack => ToRack(rack, onlineSince, portDownIds))],
                Networks = [.. networks.Select(network => new GraphNetwork { Name = network.Name, VlanId = network.VlanId, Subnet = network.Subnet ?? string.Empty })]
            };
            return new JsonResult(graph);
        }

        /// <summary>
        /// Returns each host with the services running on it.
        /// </summary>
        /// <returns>The hosts and their services as JSON.</returns>
        public async Task<JsonResult> OnGetServicesAsync()
        {
            List<Device> hosts = await context.Devices.Where(device => device.Services.Any()).Include(device => device.Services).OrderBy(device => device.Name).ToListAsync();
            DateTime onlineSince = DateTime.UtcNow.AddMinutes(-configuration.GetValue<int>("Map:OfflineAfterMinutes"));
            List<GraphHost> result = [.. hosts.Select(host => ToHost(host, onlineSince))];
            return new JsonResult(result);
        }

        /// <summary>
        /// Builds the Services view's box for a host. A synced service counts as there if it was in the host's latest report of its kind.
        /// </summary>
        /// <param name="host">The host, with its services loaded.</param>
        /// <param name="onlineSince">Devices seen since this time (UTC) count as online.</param>
        /// <returns>The host.</returns>
        private static GraphHost ToHost(Device host, DateTime onlineSince)
        {
            Dictionary<ServiceKind, DateTime?> latest = host.Services.Where(service => service.Source == ServiceSource.Synced).GroupBy(service => service.Kind).ToDictionary(group => group.Key, group => group.Max(service => service.LastSeen));
            return new GraphHost { Id = $"d{host.Id}", Label = host.Name, Online = host.LastSeen >= onlineSince, Services = [.. host.Services.OrderBy(service => service.Name).Select(service => new GraphService { Id = $"s{service.Id}", Label = service.Name, Kind = service.Kind.ToString(), Category = service.Category.ToString(), Manual = service.Source == ServiceSource.Manual, Online = service.Source == ServiceSource.Manual || service.LastSeen == latest.GetValueOrDefault(service.Kind), LastSeen = service.LastSeen == null ? string.Empty : Time(service.LastSeen.Value) })] };
        }

        /// <summary>
        /// Builds a device's node with the details every view shows.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="kind">The kind of node to draw it as.</param>
        /// <param name="online">Whether to show it as online.</param>
        /// <returns>The node.</returns>
        private static GraphNode ToNode(Device device, string kind, bool online)
        {
            return new GraphNode { Id = $"d{device.Id}", Label = device.Name, Kind = kind, Online = online, Room = device.RoomId == null ? string.Empty : $"r{device.RoomId}", Model = device.Model ?? string.Empty, Ip = device.Ip ?? string.Empty, Mac = device.Mac, LastSeen = Time(device.LastSeen) };
        }

        /// <summary>
        /// Builds a device's node for the main map, adding its rack position and ports.
        /// </summary>
        /// <param name="device">The device, with its ports, room, floor and rack loaded.</param>
        /// <param name="online">Whether to show it as online.</param>
        /// <returns>The node.</returns>
        private static GraphNode ToMapNode(Device device, bool online)
        {
            GraphNode node = ToNode(device, device.Kind.ToString().ToLowerInvariant(), online);
            node.Detail = string.Join(" · ", new[] { device.Model, device.Ip, device.Room == null ? null : $"{device.Room.Floor.Name} / {device.Room.Name}", device.Rack == null ? null : $"{device.Rack.Name} U{device.RackU}", device.Mac, $"Last Seen {Time(device.LastSeen)}" }.Where(part => !string.IsNullOrEmpty(part)));
            node.Rack = device.RackId == null ? string.Empty : $"k{device.RackId}";
            node.RackU = device.RackU ?? 0;
            node.RackColumn = device.RackColumn ?? 0;
            node.RackWidth = device.RackWidth;
            node.RackHeight = device.RackHeight;
            node.Ports = [.. device.Ports.OrderBy(port => port.Index).Select(port => new GraphPort { Id = $"p{port.Id}", Index = port.Index, Style = PortStyle.Class(port), Detail = PortStyle.Tooltip(port), Network = port.NetworkName ?? string.Empty, Tagged = port.TaggedNetworks, Name = port.Name ?? string.Empty, Up = port.Up, Speed = port.Speed, PoECapable = port.PoE, PoEPower = port.PoEPower })];
            return node;
        }

        /// <summary>
        /// Builds the edge for a cable.
        /// </summary>
        /// <param name="link">The link, with its ports and far device loaded.</param>
        /// <param name="onlineSince">Devices seen since this time (UTC) count as online.</param>
        /// <returns>The edge.</returns>
        private static GraphEdge ToEdge(Link link, DateTime onlineSince)
        {
            return new GraphEdge { Id = $"l{link.Id}", Source = $"d{link.FromPort.DeviceId}", Target = $"d{link.ToDeviceId}", SourcePort = $"p{link.FromPortId}", TargetPort = link.ToPortId != null ? $"p{link.ToPortId}" : string.Empty, Label = link.ToPort == null ? $"{link.FromPort.Index}" : $"{link.FromPort.Index} → {link.ToPort.Index}", Style = PortStyle.Class(link.FromPort), Manual = link.Source == LinkSource.Manual, PoE = PortStyle.IsPoE(link.FromPort), Splitter = link.PoESplitter, Notes = link.Notes ?? string.Empty, Online = link.ToDevice.LastSeen >= onlineSince && (link.FromPort.Up || link.FromPort.DeviceId == link.ToDeviceId), Detail = string.Join(" · ", new[] { PortStyle.Tooltip(link.FromPort), link.PoESplitter ? "PoE Splitter" : null, link.Notes }.Where(part => !string.IsNullOrEmpty(part))) };
        }

        /// <summary>
        /// Builds the Wi-Fi edge from a client's access point to the client.
        /// </summary>
        /// <param name="device">The wireless client.</param>
        /// <param name="onlineSince">Devices seen since this time (UTC) count as online.</param>
        /// <returns>The edge.</returns>
        private static GraphEdge ToWirelessEdge(Device device, DateTime onlineSince)
        {
            return new GraphEdge { Id = $"w{device.Id}", Source = $"d{device.WirelessApId}", Target = $"d{device.Id}", Label = device.WifiBand ?? "Wi-Fi", Style = "wifi", Wireless = true, Ssid = device.Ssid ?? string.Empty, Band = device.WifiBand ?? string.Empty, Channel = device.WifiChannel, Signal = device.WifiSignal, Rate = device.WifiRate, Online = device.LastSeen >= onlineSince, Detail = string.Join(" · ", new[] { device.Ssid, device.WifiBand, device.WifiChannel == null ? null : $"Channel {device.WifiChannel}", device.WifiSignal == null ? null : $"{device.WifiSignal} dBm", device.WifiRate == null ? null : $"{device.WifiRate} Mbps" }.Where(part => !string.IsNullOrEmpty(part))) };
        }

        /// <summary>
        /// Builds a rack with its panels and patches.
        /// </summary>
        /// <param name="rack">The rack, with its room, floor, devices, panels and connections loaded.</param>
        /// <param name="onlineSince">Devices seen since this time (UTC) count as online.</param>
        /// <param name="portDownIds">Devices whose cables are all down.</param>
        /// <returns>The rack.</returns>
        private static GraphRack ToRack(Rack rack, DateTime onlineSince, HashSet<int> portDownIds)
        {
            return new GraphRack { Id = $"k{rack.Id}", Name = rack.Name, Room = $"r{rack.RoomId}", HeightU = rack.HeightU, Panels = [.. rack.Panels.Select(panel => new GraphPanel { Id = $"q{panel.Id}", Kind = panel.Kind.ToString().ToLowerInvariant(), Name = panel.Name ?? $"{panel.Kind} Panel", Ports = panel.PortCount, Patches = [.. panel.Connections.Select(connection => new GraphPatch { Number = connection.Number, Side = connection.Side.ToString().ToLowerInvariant(), Node = connection.DeviceId == null ? string.Empty : $"d{connection.DeviceId}", Port = connection.PortId == null ? string.Empty : $"p{connection.PortId}", Panel = connection.TargetPanelId == null ? string.Empty : $"q{connection.TargetPanelId}", TargetNumber = connection.TargetNumber ?? 0, TargetSide = connection.TargetSide?.ToString().ToLowerInvariant() ?? string.Empty })], RackU = panel.RackU, RackColumn = panel.RackColumn, RackWidth = panel.RackWidth, RackHeight = panel.RackHeight })], Detail = $"{rack.Room.Floor.Name} / {rack.Room.Name} · {rack.HeightU}U · {(rack.Devices.Count == 0 ? "Empty" : string.Join(", ", rack.Devices.OrderByDescending(device => device.RackU).Select(device => device.Name)))}", Online = rack.Devices.Count == 0 || rack.Devices.Any(device => device.LastSeen >= onlineSince && !portDownIds.Contains(device.Id)) };
        }

        /// <summary>
        /// Formats a time for the details box.
        /// </summary>
        /// <param name="value">The time (UTC).</param>
        /// <returns>The local time as text.</returns>
        private static string Time(DateTime value) => value.ToLocalTime().ToString("d MMM HH:mm");
    }
}