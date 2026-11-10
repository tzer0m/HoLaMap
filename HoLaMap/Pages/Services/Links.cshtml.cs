using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Services
{
    /// <summary>
    /// Lists the dependencies between services, with a form to add one and buttons to delete them.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class LinksModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The service that uses the other.
        /// </summary>
        [BindProperty]
        public int? FromServiceId { get; set; }

        /// <summary>
        /// The service being used.
        /// </summary>
        [BindProperty]
        public int? ToServiceId { get; set; }

        /// <summary>
        /// Optional notes about the link.
        /// </summary>
        [BindProperty]
        public string? Notes { get; set; }

        /// <summary>
        /// What the service is used for.
        /// </summary>
        [BindProperty]
        public ServiceLinkKind Kind { get; set; }

        /// <summary>
        /// Every link, with both services and their hosts loaded.
        /// </summary>
        public List<ServiceLink> Links { get; private set; } = [];

        /// <summary>
        /// Every service, grouped by host.
        /// </summary>
        public List<SelectListItem> ServiceOptions { get; private set; } = [];

        /// <summary>
        /// Shows the links and the form.
        /// </summary>
        /// <returns>A task that completes when the page has loaded.</returns>
        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        /// <summary>
        /// Adds a link.
        /// </summary>
        /// <returns>A redirect back to the page, or the page again if the link isn't valid.</returns>
        public async Task<IActionResult> OnPostAddAsync()
        {
            if (FromServiceId == null || !await context.Services.AnyAsync(service => service.Id == FromServiceId))
            {
                ModelState.AddModelError(nameof(FromServiceId), "Choose the service that does the using.");
            }
            if (ToServiceId == null || !await context.Services.AnyAsync(service => service.Id == ToServiceId))
            {
                ModelState.AddModelError(nameof(ToServiceId), "Choose the service it uses.");
            }
            else if (ToServiceId == FromServiceId)
            {
                ModelState.AddModelError(nameof(ToServiceId), "A service can't link to itself.");
            }
            else if (await context.ServiceLinks.AnyAsync(link => link.FromServiceId == FromServiceId && link.ToServiceId == ToServiceId))
            {
                ModelState.AddModelError(nameof(ToServiceId), "Those two services are already linked.");
            }
            if (!ModelState.IsValid)
            {
                await LoadAsync();
                return Page();
            }
            context.ServiceLinks.Add(new ServiceLink { FromServiceId = FromServiceId!.Value, ToServiceId = ToServiceId!.Value, Kind = Kind, Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim() });
            await context.SaveChangesAsync();
            return RedirectToPage();
        }

        /// <summary>
        /// Changes what a link is for.
        /// </summary>
        /// <param name="id">The ID of the link.</param>
        /// <param name="kind">The new type.</param>
        /// <returns>A redirect back to the page.</returns>
        public async Task<IActionResult> OnPostKindAsync(int id, ServiceLinkKind kind)
        {
            ServiceLink? link = await context.ServiceLinks.FirstOrDefaultAsync(link => link.Id == id);
            if (link != null)
            {
                link.Kind = kind;
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Deletes a link.
        /// </summary>
        /// <param name="id">The ID of the link to delete.</param>
        /// <returns>A redirect back to the page.</returns>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            ServiceLink? link = await context.ServiceLinks.FirstOrDefaultAsync(link => link.Id == id);
            if (link != null)
            {
                context.ServiceLinks.Remove(link);
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

        /// <summary>
        /// Loads the links and the service drop-down options.
        /// </summary>
        /// <returns>A task that completes when everything has loaded.</returns>
        private async Task LoadAsync()
        {
            Links = await context.ServiceLinks.Include(link => link.FromService).ThenInclude(service => service.Device).Include(link => link.ToService).ThenInclude(service => service.Device).ToListAsync();
            Links = [.. Links.OrderBy(link => Describe(link.FromService)).ThenBy(link => Describe(link.ToService))];
            List<Service> services = await context.Services.Include(service => service.Device).ToListAsync();
            Dictionary<string, SelectListGroup> groups = services.Select(service => service.Device?.Name ?? "Cloud").Distinct().ToDictionary(name => name, name => new SelectListGroup { Name = name });
            ServiceOptions = [.. services.OrderBy(service => service.Device == null ? string.Empty : service.Device.Name).ThenBy(service => service.Group).ThenBy(service => service.Name).Select(service => new SelectListItem(string.IsNullOrEmpty(service.Group) ? service.Name : $"{service.Group} · {service.Name}", service.Id.ToString()) { Group = groups[service.Device?.Name ?? "Cloud"] })];
        }
    }
}