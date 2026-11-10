using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Services
{
    /// <summary>
    /// Adds a service by hand, or edits one that was, for things no host reports such as a script or Home Assistant.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class EditModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The ID of the service being edited, or null when adding one.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        /// <summary>
        /// The device the service runs on, 0 for the cloud, or null if none was chosen.
        /// </summary>
        [BindProperty]
        public int? DeviceId { get; set; }

        /// <summary>
        /// The service name.
        /// </summary>
        [BindProperty]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// How the service runs.
        /// </summary>
        [BindProperty]
        public ServiceKind Kind { get; set; } = ServiceKind.Other;

        /// <summary>
        /// What the service is for.
        /// </summary>
        [BindProperty]
        public ServiceCategory Category { get; set; } = ServiceCategory.Other;

        /// <summary>
        /// The group the service belongs to, if any.
        /// </summary>
        [BindProperty]
        public string? Group { get; set; }

        /// <summary>
        /// Whether it's one of your own apps.
        /// </summary>
        [BindProperty]
        public bool Mine { get; set; }

        /// <summary>
        /// Every device, for the host drop-down.
        /// </summary>
        public List<SelectListItem> DeviceOptions { get; private set; } = [];

        /// <summary>
        /// Shows the form, filled in when editing.
        /// </summary>
        /// <returns>The page, or not found if the service doesn't exist or wasn't added by hand.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            if (Id != null)
            {
                Service? service = await context.Services.FirstOrDefaultAsync(service => service.Id == Id && service.Source == ServiceSource.Manual);
                if (service == null)
                {
                    return NotFound();
                }
                DeviceId = service.DeviceId ?? 0;
                Name = service.Name;
                Kind = service.Kind;
                Category = service.Category;
                Group = service.Group;
                Mine = service.Mine;
            }
            await LoadOptionsAsync();
            return Page();
        }

        /// <summary>
        /// Saves the service.
        /// </summary>
        /// <returns>A redirect to the list, the form again if invalid, or not found if the service being edited has gone.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            string name = Name.Trim();
            string? group = string.IsNullOrWhiteSpace(Group) ? null : Group.Trim();
            // A cloud service has no device, and is always the Cloud type so it gets the cloud colour. It goes under Cloud unless it's given a category of its own.
            int? deviceId = DeviceId == 0 ? null : DeviceId;
            if (DeviceId == 0)
            {
                Kind = ServiceKind.Cloud;
                if (Category == ServiceCategory.Other)
                {
                    Category = ServiceCategory.Cloud;
                }
            }
            else if (DeviceId == null || !await context.Devices.AnyAsync(device => device.Id == DeviceId))
            {
                ModelState.AddModelError(nameof(DeviceId), "Choose the device it runs on, or Cloud.");
            }
            if (name.Length == 0)
            {
                ModelState.AddModelError(nameof(Name), "Give the service a name.");
            }
            else if (await context.Services.AnyAsync(service => service.Id != (Id ?? 0) && service.DeviceId == deviceId && service.Kind == Kind && service.Group == group && service.Name == name))
            {
                ModelState.AddModelError(nameof(Name), "That device already has a service of this type with that name.");
            }
            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();
                return Page();
            }

            Service? service = Id == null ? new Service { Source = ServiceSource.Manual } : await context.Services.FirstOrDefaultAsync(item => item.Id == Id && item.Source == ServiceSource.Manual);
            if (service == null)
            {
                return NotFound();
            }
            if (Id == null)
            {
                context.Services.Add(service);
            }
            service.DeviceId = deviceId;
            service.Name = name;
            service.Kind = Kind;
            service.Category = Category;
            service.Group = group;
            service.Mine = Mine;
            await context.SaveChangesAsync();
            return RedirectToPage("Index");
        }

        /// <summary>
        /// Loads the device drop-down options.
        /// </summary>
        /// <returns>A task that completes when the options have loaded.</returns>
        private async Task LoadOptionsAsync()
        {
            List<Device> devices = await context.Devices.OrderBy(device => device.Name).ThenBy(device => device.Mac).ToListAsync();
            DeviceOptions = [new SelectListItem("Cloud (not on a device)", "0"), .. devices.Select(device => new SelectListItem(string.Join(" · ", new[] { device.Name, device.Ip }.Where(part => !string.IsNullOrEmpty(part))), device.Id.ToString()))];
        }
    }
}