using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Floorplan
{
    /// <summary>
    /// Shows each floor as a plan of rooms with the devices inside them.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class IndexModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The ID of the floor to show, or the first floor if not set.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int? FloorId { get; set; }

        /// <summary>
        /// Every floor in display order.
        /// </summary>
        public List<Floor> Floors { get; private set; } = [];

        /// <summary>
        /// The floor being shown, if any floors exist.
        /// </summary>
        public Floor? Current { get; private set; }

        /// <summary>
        /// Loads the floors and the rooms of the current floor.
        /// </summary>
        /// <returns>A task that completes when the data has loaded.</returns>
        public async Task OnGetAsync()
        {
            Floors = await context.Floors.OrderBy(floor => floor.SortOrder).ThenBy(floor => floor.Name).ToListAsync();
            Current = Floors.FirstOrDefault(floor => floor.Id == FloorId) ?? Floors.FirstOrDefault();
            if (Current != null)
            {
                Current.Rooms = await context.Rooms.Where(room => room.FloorId == Current.Id).Include(room => room.Devices).OrderBy(room => room.Name).ToListAsync();
            }
        }

        /// <summary>
        /// Saves a room's new position and size.
        /// </summary>
        /// <param name="move">The new position and size.</param>
        /// <returns>No content, or not found if the room has gone.</returns>
        public async Task<IActionResult> OnPostMoveAsync([FromBody] RoomMove move)
        {
            Room? room = await context.Rooms.FirstOrDefaultAsync(item => item.Id == move.Id);
            if (room == null)
            {
                return NotFound();
            }
            room.X = Math.Max(0, move.X);
            room.Y = Math.Max(0, move.Y);
            room.Width = Math.Max(2, move.Width);
            room.Height = Math.Max(2, move.Height);
            await context.SaveChangesAsync();
            return new NoContentResult();
        }

        /// <summary>
        /// Adds a floor.
        /// </summary>
        /// <param name="name">The floor name.</param>
        /// <returns>A redirect to the new floor.</returns>
        public async Task<IActionResult> OnPostAddFloorAsync(string? name)
        {
            string trimmed = name?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return RedirectToPage();
            }
            Floor? existing = await context.Floors.FirstOrDefaultAsync(floor => floor.Name == trimmed);
            if (existing == null)
            {
                int next = await context.Floors.Select(floor => (int?)floor.SortOrder).MaxAsync() ?? -1;
                existing = new Floor { Name = trimmed, SortOrder = next + 1 };
                context.Floors.Add(existing);
                await context.SaveChangesAsync();
            }
            return RedirectToPage(new { floorId = existing.Id });
        }

        /// <summary>
        /// Adds a room to a floor in the next free spot.
        /// </summary>
        /// <param name="floorId">The floor to add the room to.</param>
        /// <param name="name">The room name.</param>
        /// <returns>A redirect back to the floor.</returns>
        public async Task<IActionResult> OnPostAddRoomAsync(int floorId, string? name)
        {
            string trimmed = name?.Trim() ?? string.Empty;
            bool exists = await context.Rooms.AnyAsync(room => room.FloorId == floorId && room.Name == trimmed);
            if (trimmed.Length > 0 && !exists && await context.Floors.AnyAsync(floor => floor.Id == floorId))
            {
                int count = await context.Rooms.CountAsync(room => room.FloorId == floorId);
                context.Rooms.Add(new Room { FloorId = floorId, Name = trimmed, X = count % 3 * 7, Y = count / 3 * 5 });
                await context.SaveChangesAsync();
            }
            return RedirectToPage(new { floorId });
        }

        /// <summary>
        /// Deletes a room, leaving its devices unplaced.
        /// </summary>
        /// <param name="id">The room to delete.</param>
        /// <returns>A redirect back to the floor.</returns>
        public async Task<IActionResult> OnPostDeleteRoomAsync(int id)
        {
            Room? room = await context.Rooms.FirstOrDefaultAsync(item => item.Id == id);
            if (room == null)
            {
                return RedirectToPage();
            }
            context.Rooms.Remove(room);
            await context.SaveChangesAsync();
            return RedirectToPage(new { floorId = room.FloorId });
        }

        /// <summary>
        /// Deletes a floor and its rooms, leaving their devices unplaced.
        /// </summary>
        /// <param name="id">The floor to delete.</param>
        /// <returns>A redirect to the floor plan.</returns>
        public async Task<IActionResult> OnPostDeleteFloorAsync(int id)
        {
            Floor? floor = await context.Floors.FirstOrDefaultAsync(item => item.Id == id);
            if (floor != null)
            {
                context.Floors.Remove(floor);
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}