using HoLaMap.Cloudflare;
using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Domains
{
    /// <summary>
    /// Lists each Cloudflare tunnel's public hostnames and the service each reaches, matched by port, with a pick for any it gets wrong or can't find.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class IndexModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The tunnels with their routes, in name order.
        /// </summary>
        public List<Tunnel> Tunnels { get; private set; } = [];

        /// <summary>
        /// The device each tunnel runs on, by tunnel ID, picked by hand or matched by name.
        /// </summary>
        public Dictionary<int, Device?> Hosts { get; private set; } = [];

        /// <summary>
        /// The service each route reaches by port, by route ID, whether or not one has been picked by hand.
        /// </summary>
        public Dictionary<int, Service?> Matches { get; private set; } = [];

        /// <summary>
        /// Every service, grouped by host.
        /// </summary>
        public List<SelectListItem> ServiceOptions { get; private set; } = [];

        /// <summary>
        /// Every device with services.
        /// </summary>
        public List<SelectListItem> DeviceOptions { get; private set; } = [];

        /// <summary>
        /// Shows the tunnels and their routes.
        /// </summary>
        /// <returns>A task that completes when the page has loaded.</returns>
        public async Task OnGetAsync()
        {
            List<Device> devices = await context.Devices.Where(device => device.Services.Any()).Include(device => device.Services).OrderBy(device => device.Name).ToListAsync();
            Tunnels = await context.Tunnels.Include(tunnel => tunnel.Routes).OrderBy(tunnel => tunnel.Name).ToListAsync();
            foreach (Tunnel tunnel in Tunnels)
            {
                // Routes to localhost are matched on the tunnel's device, whether picked by hand or found by name. The match is kept even when a service is picked, so the page can show it.
                Device? host = TunnelMatcher.HostOf(tunnel, devices);
                Hosts[tunnel.Id] = host;
                tunnel.Routes.ForEach(route => Matches[route.Id] = TunnelMatcher.Match(route, host, devices));
            }
            List<Service> services = await context.Services.Include(service => service.Device).ToListAsync();
            Dictionary<string, SelectListGroup> groups = services.Select(service => service.Device?.Name ?? "Cloud").Distinct().ToDictionary(name => name, name => new SelectListGroup { Name = name });
            ServiceOptions = [.. services.OrderBy(service => service.Device == null ? string.Empty : service.Device.Name).ThenBy(service => service.Group).ThenBy(service => service.Name).Select(service => new SelectListItem(string.IsNullOrEmpty(service.Group) ? service.Name : $"{service.Group} · {service.Name}", service.Id.ToString()) { Group = groups[service.Device?.Name ?? "Cloud"] })];
            DeviceOptions = [.. devices.Select(device => new SelectListItem(device.Name, device.Id.ToString()))];
        }

        /// <summary>
        /// Picks the service a route reaches, or goes back to matching it by port.
        /// </summary>
        /// <param name="id">The ID of the route.</param>
        /// <param name="serviceId">The service, or null to match by port.</param>
        /// <returns>A redirect back to the page.</returns>
        public async Task<IActionResult> OnPostServiceAsync(int id, int? serviceId)
        {
            TunnelRoute? route = await context.TunnelRoutes.FirstOrDefaultAsync(route => route.Id == id);
            if (route != null)
            {
                route.ServiceId = serviceId != null && await context.Services.AnyAsync(service => service.Id == serviceId) ? serviceId : null;
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Picks the device a tunnel runs on, or goes back to the device with the tunnel's name.
        /// </summary>
        /// <param name="id">The ID of the tunnel.</param>
        /// <param name="deviceId">The device, or null to match by name.</param>
        /// <returns>A redirect back to the page.</returns>
        public async Task<IActionResult> OnPostHostAsync(int id, int? deviceId)
        {
            Tunnel? tunnel = await context.Tunnels.FirstOrDefaultAsync(tunnel => tunnel.Id == id);
            if (tunnel != null)
            {
                tunnel.DeviceId = deviceId != null && await context.Devices.AnyAsync(device => device.Id == deviceId) ? deviceId : null;
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Gets how to show a service: its host, then its group if it has one, then its name.
        /// </summary>
        /// <param name="service">The service, with its device loaded.</param>
        /// <returns>The text to show.</returns>
        public static string Describe(Service service) => string.Join(" · ", new[] { service.Device?.Name ?? "Cloud", service.Group, service.Name }.Where(part => !string.IsNullOrEmpty(part)));
    }
}