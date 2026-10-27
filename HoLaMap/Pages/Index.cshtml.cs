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
    public class IndexModel(HoLaMapContext context) : PageModel
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
            List<Device> devices = await context.Devices.Where(device => device.Kind != DeviceKind.Client || linkedIds.Contains(device.Id)).Include(device => device.Ports).ToListAsync();
            Dictionary<int, int> upstreamPort = links.Where(link => link.FromPort.DeviceId != link.ToDeviceId).GroupBy(link => link.ToDeviceId).ToDictionary(group => group.Key, group => group.Min(link => link.FromPort.Index));
            devices = [.. devices.OrderBy(device => upstreamPort.GetValueOrDefault(device.Id)).ThenBy(device => device.Name)];
            links = [.. links.OrderBy(link => link.FromPort.Index)];
            GraphData graph = new()
            {
                Nodes = [.. devices.Select(device => new GraphNode { Id = $"d{device.Id}", Label = device.Name, Kind = device.Kind.ToString().ToLowerInvariant(), Detail = string.Join(" · ", new[] { device.Model, device.Ip, device.Mac }.Where(part => !string.IsNullOrEmpty(part))), Ports = [.. device.Ports.OrderBy(port => port.Index).Select(port => new GraphPort { Id = $"p{port.Id}", Index = port.Index, Style = PortStyle.Class(port), Detail = PortStyle.Tooltip(port) })] })],
                Edges = [.. links.Select(link => new GraphEdge { Id = $"l{link.Id}", Source = $"d{link.FromPort.DeviceId}", Target = $"d{link.ToDeviceId}", SourcePort = $"p{link.FromPortId}", TargetPort = link.ToPortId != null ? $"p{link.ToPortId}" : string.Empty, Label = link.ToPort == null ? $"{link.FromPort.Index}" : $"{link.FromPort.Index} → {link.ToPort.Index}", Style = PortStyle.Class(link.FromPort), Manual = link.Source == LinkSource.Manual, PoE = PortStyle.IsPoE(link.FromPort), Detail = string.IsNullOrEmpty(link.Notes) ? PortStyle.Tooltip(link.FromPort) : $"{PortStyle.Tooltip(link.FromPort)} · {link.Notes}" })]
            };
            return new JsonResult(graph);
        }
    }
}