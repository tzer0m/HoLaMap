using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Actions
{
    /// <summary>
    /// Lists devices seen on ports that are already assigned, and resolves them.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class IndexModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The open action items.
        /// </summary>
        public List<ActionItem> Items { get; private set; } = [];

        /// <summary>
        /// The current links on each item's port, keyed by port ID.
        /// </summary>
        public Dictionary<int, List<Link>> CurrentLinks { get; private set; } = [];

        /// <summary>
        /// Loads the open action items.
        /// </summary>
        /// <returns>A task that completes when the items have loaded.</returns>
        public async Task OnGetAsync()
        {
            Items = await context.ActionItems.Where(item => !item.Dismissed).Include(item => item.Port).ThenInclude(port => port.Device).Include(item => item.Device).OrderBy(item => item.FirstSeen).ToListAsync();
            List<int> portIds = [.. Items.Select(item => item.PortId)];
            List<Link> links = await context.Links.Where(link => portIds.Contains(link.FromPortId)).Include(link => link.ToDevice).ToListAsync();
            CurrentLinks = links.GroupBy(link => link.FromPortId).ToDictionary(group => group.Key, group => group.OrderBy(link => link.ToDevice.Name).ToList());
        }

        /// <summary>
        /// Gives the port to the new device, removing whatever was assigned to it.
        /// </summary>
        /// <param name="id">The action item ID.</param>
        /// <returns>A redirect back to the list.</returns>
        public async Task<IActionResult> OnPostReplaceAsync(int id)
        {
            ActionItem? item = await context.ActionItems.Include(item => item.Port).FirstOrDefaultAsync(item => item.Id == id);
            if (item != null)
            {
                context.Links.RemoveRange(await context.Links.Where(link => link.FromPortId == item.PortId).ToListAsync());
                await AssignAsync(item);
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Adds the new device to the port alongside what is already there.
        /// </summary>
        /// <param name="id">The action item ID.</param>
        /// <param name="markShared">Whether to mark the port as shared so future devices are added automatically.</param>
        /// <returns>A redirect back to the list.</returns>
        public async Task<IActionResult> OnPostAlongsideAsync(int id, bool markShared)
        {
            ActionItem? item = await context.ActionItems.Include(item => item.Port).FirstOrDefaultAsync(item => item.Id == id);
            if (item != null)
            {
                item.Port.Shared = item.Port.Shared || markShared;
                await AssignAsync(item);
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Dismisses an action item without changing anything.
        /// </summary>
        /// <param name="id">The action item ID.</param>
        /// <returns>A redirect back to the list.</returns>
        public async Task<IActionResult> OnPostDismissAsync(int id)
        {
            ActionItem? item = await context.ActionItems.FirstOrDefaultAsync(item => item.Id == id);
            if (item != null)
            {
                item.Dismissed = true;
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Moves the item's device onto its port and clears its action items.
        /// </summary>
        /// <param name="item">The action item being resolved.</param>
        /// <returns>A task that completes when the change has been saved.</returns>
        private async Task AssignAsync(ActionItem item)
        {
            context.Links.RemoveRange(await context.Links.Where(link => link.ToDeviceId == item.DeviceId && link.Source == LinkSource.UniFi).ToListAsync());
            context.Links.Add(new Link { FromPortId = item.PortId, ToDeviceId = item.DeviceId, Source = LinkSource.UniFi, LastSeen = item.LastSeen });
            context.ActionItems.RemoveRange(await context.ActionItems.Where(other => other.DeviceId == item.DeviceId).ToListAsync());
            await context.SaveChangesAsync();
        }
    }
}