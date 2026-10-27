using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages
{
    /// <summary>
    /// Shows every gateway, switch and access point with its ports and what is connected to them.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class IndexModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// Gateways, switches and access points in display order.
        /// </summary>
        public List<Device> Infrastructure { get; private set; } = [];

        /// <summary>
        /// Links leaving each port, keyed by port ID.
        /// </summary>
        public Dictionary<int, List<Link>> Outgoing { get; private set; } = [];

        /// <summary>
        /// The link arriving at each port, keyed by port ID.
        /// </summary>
        public Dictionary<int, Link> Incoming { get; private set; } = [];

        /// <summary>
        /// Loads the devices, ports and links.
        /// </summary>
        /// <returns>A task that completes when the data has loaded.</returns>
        public async Task OnGetAsync()
        {
            List<Device> devices = await context.Devices.Where(device => device.Kind != DeviceKind.Client).Include(device => device.Ports).ToListAsync();
            Infrastructure = devices.OrderBy(device => device.Kind).ThenBy(device => device.Name).ToList();
            List<Link> links = await context.Links.Include(link => link.FromPort).ThenInclude(port => port.Device).Include(link => link.ToDevice).Include(link => link.ToPort).ToListAsync();
            Outgoing = links.GroupBy(link => link.FromPortId).ToDictionary(group => group.Key, group => group.OrderBy(link => link.ToDevice.Name).ToList());
            Incoming = links.Where(link => link.ToPortId != null).GroupBy(link => link.ToPortId!.Value).ToDictionary(group => group.Key, group => group.First());
        }

        /// <summary>
        /// Gets the CSS class for a port based on its state, speed and PoE.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns>The CSS class.</returns>
        public string PortClass(Port port)
        {
            if (!port.Up)
            {
                return "port-down";
            }
            if (IsWan(port))
            {
                return "port-wan";
            }
            if (port.Speed < 1000)
            {
                return "port-fe";
            }
            return port.PoE ? "port-gbe-poe" : "port-gbe";
        }

        /// <summary>
        /// Gets the names of what is connected to a port.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns>The connected device names, one per entry.</returns>
        public List<string> Connections(Port port)
        {
            List<string> names = [];
            if (Incoming.TryGetValue(port.Id, out Link? incoming))
            {
                names.Add($"↑ {incoming.FromPort.Device.Name} Port {incoming.FromPort.Index}");
            }
            if (Outgoing.TryGetValue(port.Id, out List<Link>? outgoing))
            {
                names.AddRange(outgoing.Select(link => link.ToPort == null ? link.ToDevice.Name : $"{link.ToDevice.Name} Port {link.ToPort.Index}"));
            }
            return names;
        }

        /// <summary>
        /// Gets whether a port carries the internet connection.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns>True if the port is a WAN port.</returns>
        private static bool IsWan(Port port)
        {
            return (port.Name?.Contains("WAN", StringComparison.OrdinalIgnoreCase) ?? false) || (port.NetworkName?.StartsWith("WAN", StringComparison.OrdinalIgnoreCase) ?? false) || (port.NetworkName?.StartsWith("Internet", StringComparison.OrdinalIgnoreCase) ?? false);
        }
    }
}