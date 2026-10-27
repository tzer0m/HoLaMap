using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Links
{
    /// <summary>
    /// Adds a link that UniFi can't detect.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class CreateModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The port the link starts from.
        /// </summary>
        [BindProperty]
        public int FromPortId { get; set; }

        /// <summary>
        /// The link's destination, as device:ID or port:ID.
        /// </summary>
        [BindProperty]
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Optional notes about the link.
        /// </summary>
        [BindProperty]
        public string? Notes { get; set; }

        /// <summary>
        /// Every port, grouped by device.
        /// </summary>
        public List<SelectListItem> PortOptions { get; private set; } = [];

        /// <summary>
        /// Every device and port, grouped by device.
        /// </summary>
        public List<SelectListItem> TargetOptions { get; private set; } = [];

        /// <summary>
        /// Shows the form.
        /// </summary>
        /// <returns>A task that completes when the options have loaded.</returns>
        public async Task OnGetAsync()
        {
            await LoadOptionsAsync();
        }

        /// <summary>
        /// Saves the new link.
        /// </summary>
        /// <returns>A redirect to the list, or the form again if invalid.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            Port? fromPort = await context.Ports.FirstOrDefaultAsync(port => port.Id == FromPortId);
            string[] parts = Target.Split(':');
            int targetId = 0;
            if (fromPort == null)
            {
                ModelState.AddModelError(nameof(FromPortId), "Choose a port to link from.");
            }
            if (parts.Length != 2 || !int.TryParse(parts[1], out targetId))
            {
                ModelState.AddModelError(nameof(Target), "Choose a device or port to link to.");
            }
            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();
                return Page();
            }

            Link link = new() { FromPortId = fromPort!.Id, Source = LinkSource.Manual, Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim() };
            if (parts[0] == "port")
            {
                Port? toPort = await context.Ports.FirstOrDefaultAsync(port => port.Id == targetId);
                link.ToPortId = toPort?.Id;
                link.ToDeviceId = toPort?.DeviceId ?? 0;
            }
            else
            {
                link.ToDeviceId = await context.Devices.Where(device => device.Id == targetId).Select(device => device.Id).FirstOrDefaultAsync();
            }
            if (link.ToDeviceId == 0)
            {
                ModelState.AddModelError(nameof(Target), "That device or port no longer exists.");
                await LoadOptionsAsync();
                return Page();
            }
            context.Links.Add(link);
            await context.SaveChangesAsync();
            return RedirectToPage("Index");
        }

        /// <summary>
        /// Loads the port and target drop-down options.
        /// </summary>
        /// <returns>A task that completes when the options have loaded.</returns>
        private async Task LoadOptionsAsync()
        {
            List<Device> devices = await context.Devices.Include(device => device.Ports).ToListAsync();
            PortOptions = [];
            TargetOptions = [];
            foreach (Device device in devices.OrderBy(device => device.Kind).ThenBy(device => device.Name))
            {
                SelectListGroup group = new() { Name = device.Name };
                TargetOptions.Add(new SelectListItem($"{device.Name} (No Port)", $"device:{device.Id}") { Group = group });
                foreach (Port port in device.Ports.OrderBy(port => port.Index))
                {
                    string text = string.IsNullOrEmpty(port.Name) ? $"Port {port.Index}" : $"Port {port.Index} · {port.Name}";
                    PortOptions.Add(new SelectListItem(text, port.Id.ToString()) { Group = group });
                    TargetOptions.Add(new SelectListItem(text, $"port:{port.Id}") { Group = group });
                }
            }
        }
    }
}