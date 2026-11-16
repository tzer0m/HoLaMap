using System.Globalization;
using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Power
{
    /// <summary>
    /// Adds a machine to the Power page, or changes how it's checked, woken and powered off.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class EditModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The device being set up.
        /// </summary>
        [BindProperty]
        public int? DeviceId { get; set; }

        /// <summary>
        /// The MAC address to wake, when the device has more than one.
        /// </summary>
        [BindProperty]
        public string? WakeMac { get; set; }

        /// <summary>
        /// The network interface the wake packet is sent from.
        /// </summary>
        [BindProperty]
        public string? WakeInterface { get; set; }

        /// <summary>
        /// The TCP port checked to see if the device is up, or empty to ping it.
        /// </summary>
        [BindProperty]
        public int? PowerCheckPort { get; set; }

        /// <summary>
        /// The device's host name in Semaphore.
        /// </summary>
        [BindProperty]
        public string? SemaphoreHost { get; set; }

        /// <summary>
        /// The Semaphore inventory that holds the device.
        /// </summary>
        [BindProperty]
        public int? SemaphoreInventoryId { get; set; }

        /// <summary>
        /// Whether the device can be put to sleep.
        /// </summary>
        [BindProperty]
        public bool CanSleep { get; set; }

        /// <summary>
        /// The machine being edited, or null when adding one.
        /// </summary>
        public Device? Machine { get; private set; }

        /// <summary>
        /// The devices that can be added.
        /// </summary>
        public List<SelectListItem> DeviceOptions { get; private set; } = [];

        /// <summary>
        /// The machine's MAC addresses, its own first.
        /// </summary>
        public List<SelectListItem> MacOptions { get; private set; } = [];

        /// <summary>
        /// Shows the form, filled in when editing a machine.
        /// </summary>
        /// <param name="id">The device ID, or null to add a machine.</param>
        /// <returns>The page, or not found if the machine isn't on the Power page.</returns>
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id != null)
            {
                Machine = await context.Devices.Include(device => device.Macs).FirstOrDefaultAsync(device => device.Id == id && device.PowerControl);
                if (Machine == null)
                {
                    return NotFound();
                }
                DeviceId = Machine.Id;
                WakeMac = Machine.WakeMac;
                WakeInterface = Machine.WakeInterface;
                PowerCheckPort = Machine.PowerCheckPort;
                SemaphoreHost = Machine.SemaphoreHost;
                SemaphoreInventoryId = Machine.SemaphoreInventoryId;
                CanSleep = Machine.CanSleep;
            }
            await LoadOptionsAsync();
            return Page();
        }

        /// <summary>
        /// Saves the machine's settings and puts it on the Power page.
        /// </summary>
        /// <returns>A redirect to the Power page, or the form again if invalid.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            Device? device = await context.Devices.Include(device => device.Macs).FirstOrDefaultAsync(device => device.Id == DeviceId);
            if (device == null)
            {
                ModelState.AddModelError(nameof(DeviceId), "Choose a device.");
            }
            if (string.IsNullOrWhiteSpace(WakeInterface) && string.IsNullOrWhiteSpace(SemaphoreHost))
            {
                ModelState.AddModelError(string.Empty, "Set a wake interface, a Semaphore host or both.");
            }
            if (device == null || !ModelState.IsValid)
            {
                Machine = device?.PowerControl == true ? device : null;
                await LoadOptionsAsync();
                return Page();
            }
            List<string> macs = [device.Mac, .. device.Macs.Select(mac => mac.Mac)];
            device.PowerControl = true;
            device.WakeMac = WakeMac != null && WakeMac != device.Mac && macs.Contains(WakeMac) ? WakeMac : null;
            device.WakeInterface = Clean(WakeInterface);
            device.PowerCheckPort = PowerCheckPort;
            device.SemaphoreHost = Clean(SemaphoreHost);
            device.SemaphoreInventoryId = SemaphoreInventoryId;
            device.CanSleep = CanSleep;
            await context.SaveChangesAsync();
            return RedirectToPage("Index");
        }

        /// <summary>
        /// Takes a machine off the Power page and clears its settings.
        /// </summary>
        /// <param name="id">The device ID.</param>
        /// <returns>A redirect to the Power page.</returns>
        public async Task<IActionResult> OnPostRemoveAsync(int id)
        {
            Device? device = await context.Devices.FirstOrDefaultAsync(device => device.Id == id);
            if (device != null)
            {
                device.PowerControl = false;
                device.WakeMac = null;
                device.WakeInterface = null;
                device.PowerCheckPort = null;
                device.SemaphoreHost = null;
                device.SemaphoreInventoryId = null;
                device.CanSleep = false;
                await context.SaveChangesAsync();
            }
            return RedirectToPage("Index");
        }

        /// <summary>
        /// Loads the devices that can be added, or the MAC addresses of the machine being edited.
        /// </summary>
        /// <returns>A task that completes when the options have loaded.</returns>
        private async Task LoadOptionsAsync()
        {
            if (Machine != null)
            {
                MacOptions = [new SelectListItem(Machine.Mac, Machine.Mac), .. Machine.Macs.Select(mac => new SelectListItem(mac.Mac, mac.Mac))];
                return;
            }
            List<Device> devices = await context.Devices.Where(device => !device.PowerControl).OrderBy(device => device.Name).ToListAsync();
            DeviceOptions = [.. devices.Select(device => new SelectListItem($"{device.Name} · {device.Ip} · {device.Mac}", device.Id.ToString(CultureInfo.InvariantCulture)))];
        }

        /// <summary>
        /// Trims a setting, treating a blank one as not set.
        /// </summary>
        /// <param name="value">The value entered.</param>
        /// <returns>The trimmed value, or null if blank.</returns>
        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}