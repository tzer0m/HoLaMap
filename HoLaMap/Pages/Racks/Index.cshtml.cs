using HoLaMap.Data;
using HoLaMap.Display;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Racks
{
    /// <summary>
    /// Lists the racks and adds new ones.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class IndexModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// Every rack in floor, room then name order.
        /// </summary>
        public List<Rack> Racks { get; private set; } = [];

        /// <summary>
        /// The rooms to choose from, grouped by floor.
        /// </summary>
        public List<SelectListItem> RoomOptions { get; private set; } = [];

        /// <summary>
        /// Loads the racks and rooms.
        /// </summary>
        /// <returns>A task that completes when the data has loaded.</returns>
        public async Task OnGetAsync()
        {
            List<Rack> racks = await context.Racks.Include(rack => rack.Room).ThenInclude(room => room.Floor).Include(rack => rack.Devices).ToListAsync();
            Racks = [.. racks.OrderBy(rack => rack.Room.Floor.SortOrder).ThenBy(rack => rack.Room.Name).ThenBy(rack => rack.Name)];
            RoomOptions = await RoomChoices.LoadAsync(context);
        }

        /// <summary>
        /// Adds a rack to a room.
        /// </summary>
        /// <param name="name">The rack name.</param>
        /// <param name="roomId">The room the rack is in.</param>
        /// <param name="heightU">The height in rack units.</param>
        /// <returns>A redirect to the new rack, or back to the list if invalid.</returns>
        public async Task<IActionResult> OnPostAddAsync(string? name, int roomId, int heightU)
        {
            string trimmed = name?.Trim() ?? string.Empty;
            bool exists = await context.Racks.AnyAsync(rack => rack.RoomId == roomId && rack.Name == trimmed);
            if (trimmed.Length == 0 || exists || heightU < 1 || !await context.Rooms.AnyAsync(room => room.Id == roomId))
            {
                return RedirectToPage();
            }
            Rack created = new() { Name = trimmed, RoomId = roomId, HeightU = heightU };
            context.Racks.Add(created);
            await context.SaveChangesAsync();
            return RedirectToPage("Details", new { id = created.Id });
        }

        /// <summary>
        /// Deletes a rack, leaving its devices in the room but out of the rack.
        /// </summary>
        /// <param name="id">The rack to delete.</param>
        /// <returns>A redirect back to the list.</returns>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            Rack? rack = await context.Racks.Include(item => item.Devices).FirstOrDefaultAsync(item => item.Id == id);
            if (rack != null)
            {
                foreach (Device device in rack.Devices)
                {
                    device.RackId = null;
                    device.RackU = null;
                    device.RackColumn = null;
                }
                context.Racks.Remove(rack);
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}