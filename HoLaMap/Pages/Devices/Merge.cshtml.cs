using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HoLaMap.Pages.Devices
{
    /// <summary>
    /// Merges duplicate devices, such as one device reporting two MAC addresses.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class MergeModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The device to keep.
        /// </summary>
        [BindProperty]
        public int KeepId { get; set; }

        /// <summary>
        /// The devices to merge into the kept device.
        /// </summary>
        [BindProperty]
        public List<int> MergeIds { get; set; } = [];

        /// <summary>
        /// Every device, with its MAC and IP so duplicates can be told apart.
        /// </summary>
        public List<SelectListItem> DeviceOptions { get; private set; } = [];

        /// <summary>
        /// Shows the form.
        /// </summary>
        /// <returns>A task that completes when the options have loaded.</returns>
        public async Task OnGetAsync()
        {
            await LoadOptionsAsync();
        }

        /// <summary>
        /// Merges the chosen devices into the kept device.
        /// </summary>
        /// <returns>A redirect to the map, or the form again if invalid.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            Device? keep = await context.Devices.FirstOrDefaultAsync(device => device.Id == KeepId);
            List<Device> merging = await context.Devices.Include(device => device.Ports).Include(device => device.Macs).Where(device => MergeIds.Contains(device.Id) && device.Id != KeepId).ToListAsync();
            if (keep == null)
            {
                ModelState.AddModelError(nameof(KeepId), "Choose the device to keep.");
            }
            if (merging.Count == 0)
            {
                ModelState.AddModelError(nameof(MergeIds), "Choose at least one other device to merge into it.");
            }
            if (merging.Any(device => device.Ports.Count > 0))
            {
                ModelState.AddModelError(nameof(MergeIds), "Devices with ports (switches, APs, the gateway) can't be merged into another device.");
            }
            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();
                return Page();
            }

            // Move links onto the kept device, dropping any that would duplicate one it already has.
            List<int> mergingIds = [.. merging.Select(device => device.Id)];
            List<Link> links = await context.Links.Where(link => mergingIds.Contains(link.ToDeviceId) || link.ToDeviceId == keep!.Id).ToListAsync();
            foreach (Link link in links.Where(link => mergingIds.Contains(link.ToDeviceId)).ToList())
            {
                if (links.Any(other => other.ToDeviceId == keep!.Id && other.FromPortId == link.FromPortId))
                {
                    context.Links.Remove(link);
                }
                else
                {
                    link.ToDeviceId = keep!.Id;
                }
            }
            context.ActionItems.RemoveRange(await context.ActionItems.Where(item => mergingIds.Contains(item.DeviceId)).ToListAsync());

            // Remove the merged devices first so their MACs are free to move to the kept device.
            List<string> macs = [.. merging.SelectMany(device => device.Macs.Select(deviceMac => deviceMac.Mac).Prepend(device.Mac))];
            keep!.LastSeen = merging.Select(device => device.LastSeen).Append(keep.LastSeen).Max();
            context.Devices.RemoveRange(merging);
            await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();
            await context.SaveChangesAsync();
            context.DeviceMacs.AddRange(macs.Select(mac => new DeviceMac { DeviceId = keep.Id, Mac = mac }));
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToPage("/Index");
        }

        /// <summary>
        /// Loads the device drop-down options.
        /// </summary>
        /// <returns>A task that completes when the options have loaded.</returns>
        private async Task LoadOptionsAsync()
        {
            List<Device> devices = await context.Devices.OrderBy(device => device.Name).ThenBy(device => device.Mac).ToListAsync();
            DeviceOptions = [.. devices.Select(device => new SelectListItem(string.Join(" · ", new[] { device.Name, device.Mac, device.Ip }.Where(part => !string.IsNullOrEmpty(part))), device.Id.ToString()))];
        }
    }
}