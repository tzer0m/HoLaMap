using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Floorplan
{
    /// <summary>
    /// Assigns devices to rooms. The assignment is never changed by the UniFi sync.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class AssignModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// Every device in display order.
        /// </summary>
        public List<Device> Devices { get; private set; } = [];

        /// <summary>
        /// The rooms to choose from, grouped by floor.
        /// </summary>
        public List<SelectListItem> RoomOptions { get; private set; } = [];

        /// <summary>
        /// The Zigbee devices.
        /// </summary>
        public List<ZigbeeDevice> ZigbeeDevices { get; private set; } = [];

        /// <summary>
        /// Loads the devices and rooms.
        /// </summary>
        /// <returns>A task that completes when the data has loaded.</returns>
        public async Task OnGetAsync()
        {
            List<Device> devices = await context.Devices.ToListAsync();
            Devices = [.. devices.OrderBy(device => device.Kind == DeviceKind.Client).ThenBy(device => device.Kind).ThenBy(device => device.Name)];
            ZigbeeDevices = await context.ZigbeeDevices.OrderBy(device => device.Role).ThenBy(device => device.Name).ToListAsync();
            List<Room> rooms = await context.Rooms.Include(room => room.Floor).ToListAsync();
            Dictionary<int, SelectListGroup> groups = rooms.Select(room => room.Floor).DistinctBy(floor => floor.Id).ToDictionary(floor => floor.Id, floor => new SelectListGroup { Name = floor.Name });
            RoomOptions = [.. rooms.OrderBy(room => room.Floor.SortOrder).ThenBy(room => room.Name).Select(room => new SelectListItem(room.Name, room.Id.ToString()) { Group = groups[room.FloorId] })];
        }

        /// <summary>
        /// Moves a device into a room, or out of its room.
        /// </summary>
        /// <param name="id">The device ID.</param>
        /// <param name="roomId">The room ID, or null for no room.</param>
        /// <returns>A redirect back to the list.</returns>
        public async Task<IActionResult> OnPostSaveAsync(int id, int? roomId)
        {
            Device? device = await context.Devices.Include(item => item.Rack).FirstOrDefaultAsync(item => item.Id == id);
            if (device != null && (roomId == null || await context.Rooms.AnyAsync(room => room.Id == roomId)))
            {
                if (device.Rack != null && device.Rack.RoomId != roomId)
                {
                    device.RackId = null;
                    device.RackU = null;
                    device.RackColumn = null;
                }
                device.RoomId = roomId;
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Moves a Zigbee device into a room, or out of its room.
        /// </summary>
        /// <param name="id">The Zigbee device ID.</param>
        /// <param name="roomId">The room ID, or null for no room.</param>
        /// <returns>A redirect back to the list.</returns>
        public async Task<IActionResult> OnPostSaveZigbeeAsync(int id, int? roomId)
        {
            ZigbeeDevice? device = await context.ZigbeeDevices.FirstOrDefaultAsync(item => item.Id == id);
            if (device != null && (roomId == null || await context.Rooms.AnyAsync(room => room.Id == roomId)))
            {
                device.RoomId = roomId;
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}