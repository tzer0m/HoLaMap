using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Display
{
    /// <summary>
    /// Builds room drop-down options grouped by floor.
    /// </summary>
    public static class RoomChoices
    {
        /// <summary>
        /// Loads every room as a drop-down option, grouped by floor.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <returns>The options in floor then room order.</returns>
        public static async Task<List<SelectListItem>> LoadAsync(HoLaMapContext context)
        {
            List<Room> rooms = await context.Rooms.Include(room => room.Floor).ToListAsync();
            Dictionary<int, SelectListGroup> groups = rooms.Select(room => room.Floor).DistinctBy(floor => floor.Id).ToDictionary(floor => floor.Id, floor => new SelectListGroup { Name = floor.Name });
            return [.. rooms.OrderBy(room => room.Floor.SortOrder).ThenBy(room => room.Name).Select(room => new SelectListItem(room.Name, room.Id.ToString()) { Group = groups[room.FloorId] })];
        }
    }
}