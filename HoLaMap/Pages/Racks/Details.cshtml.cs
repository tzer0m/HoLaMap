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
    /// Shows a rack as a grid of units and four columns, and places devices in it.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class DetailsModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The ID of the placed device being edited.
        /// </summary>
        [BindProperty(SupportsGet = true, Name = "edit")]
        public int? EditId { get; set; }

        /// <summary>
        /// The ID of the panel being edited.
        /// </summary>
        [BindProperty(SupportsGet = true, Name = "panel")]
        public int? EditPanelId { get; set; }

        /// <summary>
        /// The rack being shown.
        /// </summary>
        public Rack Rack { get; private set; } = null!;

        /// <summary>
        /// The devices in the rack.
        /// </summary>
        public List<Device> Placed { get; private set; } = [];

        /// <summary>
        /// The placed device being edited, if any.
        /// </summary>
        public Device? Editing { get; private set; }

        /// <summary>
        /// The blank and brush panels in the rack.
        /// </summary>
        public List<RackPanel> Panels { get; private set; } = [];

        /// <summary>
        /// The panel being edited, if any.
        /// </summary>
        public RackPanel? EditingPanel { get; private set; }

        /// <summary>
        /// The devices that can be added to the rack.
        /// </summary>
        public List<Device> Candidates { get; private set; } = [];

        /// <summary>
        /// The rooms to choose from, grouped by floor.
        /// </summary>
        public List<SelectListItem> RoomOptions { get; private set; } = [];

        /// <summary>
        /// A message explaining why the last change was refused.
        /// </summary>
        public string? Error { get; private set; }

        /// <summary>
        /// Shows the rack.
        /// </summary>
        /// <param name="id">The rack ID.</param>
        /// <returns>The page, or not found if the rack has gone.</returns>
        public async Task<IActionResult> OnGetAsync(int id)
        {
            return await LoadAsync(id) ? Page() : NotFound();
        }

        /// <summary>
        /// Places a device in the rack, or moves it within the rack.
        /// </summary>
        /// <param name="id">The rack ID.</param>
        /// <param name="deviceId">The device to place.</param>
        /// <param name="u">The lowest unit the device occupies.</param>
        /// <param name="column">The left column from 0 to 3.</param>
        /// <param name="width">The width in columns: 1, 2 or 4.</param>
        /// <param name="height">The height in units.</param>
        /// <returns>A redirect back to the rack, or the page again if the spot is not valid.</returns>
        public async Task<IActionResult> OnPostPlaceAsync(int id, int deviceId, int u, int column, int width, int height)
        {
            if (!await LoadAsync(id))
            {
                return NotFound();
            }
            Device? device = await context.Devices.FirstOrDefaultAsync(item => item.Id == deviceId);
            if (device == null)
            {
                return RedirectToPage(new { id });
            }
            string? problem = Check(device.Id, null, u, column, width, height);
            if (problem != null)
            {
                Error = problem;
                EditId = device.RackId == id ? device.Id : null;
                Editing = Placed.FirstOrDefault(item => item.Id == EditId);
                return Page();
            }
            device.RackId = id;
            device.RoomId = Rack.RoomId;
            device.RackU = u;
            device.RackColumn = column;
            device.RackWidth = width;
            device.RackHeight = height;
            await context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        /// <summary>
        /// Takes a device out of the rack, leaving it in the room.
        /// </summary>
        /// <param name="id">The rack ID.</param>
        /// <param name="deviceId">The device to remove.</param>
        /// <returns>A redirect back to the rack.</returns>
        public async Task<IActionResult> OnPostRemoveAsync(int id, int deviceId)
        {
            Device? device = await context.Devices.FirstOrDefaultAsync(item => item.Id == deviceId && item.RackId == id);
            if (device != null)
            {
                device.RackId = null;
                device.RackU = null;
                device.RackColumn = null;
                await context.SaveChangesAsync();
            }
            return RedirectToPage(new { id });
        }

        /// <summary>
        /// Adds a blank or brush panel to the rack, or changes one already in it.
        /// </summary>
        /// <param name="id">The rack ID.</param>
        /// <param name="panelId">The panel to change, or null to add a new one.</param>
        /// <param name="kind">The kind of panel.</param>
        /// <param name="u">The lowest unit the panel occupies.</param>
        /// <param name="column">The left column from 0 to 3.</param>
        /// <param name="width">The width in columns: 1, 2 or 4.</param>
        /// <param name="height">The height in units.</param>
        /// <returns>A redirect back to the rack, or the page again if the spot is not valid.</returns>
        public async Task<IActionResult> OnPostPlacePanelAsync(int id, int? panelId, RackPanelKind kind, int u, int column, int width, int height)
        {
            if (!await LoadAsync(id))
            {
                return NotFound();
            }
            RackPanel? panel = panelId == null ? new RackPanel { RackId = id } : await context.RackPanels.FirstOrDefaultAsync(item => item.Id == panelId && item.RackId == id);
            if (panel == null)
            {
                return RedirectToPage(new { id });
            }
            string? problem = Check(null, panelId, u, column, width, height);
            if (problem != null)
            {
                Error = problem;
                EditPanelId = panelId;
                EditingPanel = Panels.FirstOrDefault(item => item.Id == panelId);
                return Page();
            }
            panel.Kind = kind;
            panel.RackU = u;
            panel.RackColumn = column;
            panel.RackWidth = width;
            panel.RackHeight = height;
            if (panelId == null)
            {
                context.RackPanels.Add(panel);
            }
            await context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        /// <summary>
        /// Takes a panel out of the rack.
        /// </summary>
        /// <param name="id">The rack ID.</param>
        /// <param name="panelId">The panel to remove.</param>
        /// <returns>A redirect back to the rack.</returns>
        public async Task<IActionResult> OnPostRemovePanelAsync(int id, int panelId)
        {
            RackPanel? panel = await context.RackPanels.FirstOrDefaultAsync(item => item.Id == panelId && item.RackId == id);
            if (panel != null)
            {
                context.RackPanels.Remove(panel);
                await context.SaveChangesAsync();
            }
            return RedirectToPage(new { id });
        }

        /// <summary>
        /// Changes the rack's name, room or height.
        /// </summary>
        /// <param name="id">The rack ID.</param>
        /// <param name="name">The new name.</param>
        /// <param name="roomId">The new room.</param>
        /// <param name="heightU">The new height in units.</param>
        /// <returns>A redirect back to the rack, or the page again if the change is not valid.</returns>
        public async Task<IActionResult> OnPostSaveRackAsync(int id, string? name, int roomId, int heightU)
        {
            if (!await LoadAsync(id))
            {
                return NotFound();
            }
            string trimmed = name?.Trim() ?? string.Empty;
            int highest = Placed.Select(device => device.RackU!.Value + device.RackHeight - 1).Concat(Panels.Select(panel => panel.RackU + panel.RackHeight - 1)).DefaultIfEmpty(0).Max();
            bool clash = await context.Racks.AnyAsync(rack => rack.Id != id && rack.RoomId == roomId && rack.Name == trimmed);
            if (trimmed.Length == 0 || clash || !await context.Rooms.AnyAsync(room => room.Id == roomId))
            {
                Error = "Give the rack a name that isn't used by another rack in that room.";
                return Page();
            }
            if (heightU < Math.Max(1, highest))
            {
                Error = $"Devices or panels reach unit {highest}, so the rack can't be shorter than that.";
                return Page();
            }
            Rack existing = await context.Racks.Include(item => item.Devices).FirstAsync(item => item.Id == id);
            existing.Name = trimmed;
            existing.HeightU = heightU;
            if (existing.RoomId != roomId)
            {
                existing.RoomId = roomId;
                foreach (Device device in existing.Devices)
                {
                    device.RoomId = roomId;
                }
            }
            await context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        /// <summary>
        /// Loads the rack, its devices and the options for the forms.
        /// </summary>
        /// <param name="id">The rack ID.</param>
        /// <returns>False if the rack doesn't exist.</returns>
        private async Task<bool> LoadAsync(int id)
        {
            Rack? rack = await context.Racks.Include(item => item.Room).ThenInclude(room => room.Floor).FirstOrDefaultAsync(item => item.Id == id);
            if (rack == null)
            {
                return false;
            }
            Rack = rack;
            List<Device> placed = await context.Devices.Where(device => device.RackId == id).ToListAsync();
            Placed = [.. placed.OrderByDescending(device => device.RackU).ThenBy(device => device.RackColumn)];
            Editing = Placed.FirstOrDefault(device => device.Id == EditId);
            List<RackPanel> panels = await context.RackPanels.Where(panel => panel.RackId == id).ToListAsync();
            Panels = [.. panels.OrderByDescending(panel => panel.RackU).ThenBy(panel => panel.RackColumn)];
            EditingPanel = Panels.FirstOrDefault(panel => panel.Id == EditPanelId);
            List<Device> others = await context.Devices.Where(device => device.RackId == null || device.RackId != id).Include(device => device.Rack).ToListAsync();
            Candidates = [.. others.OrderBy(device => device.Kind == DeviceKind.Client).ThenBy(device => device.Kind).ThenBy(device => device.Name)];
            RoomOptions = await RoomChoices.LoadAsync(context);
            return true;
        }

        /// <summary>
        /// Checks that a device or panel fits in the rack at the given spot without touching another device or panel.
        /// </summary>
        /// <param name="deviceId">The device being placed, if it's a device.</param>
        /// <param name="panelId">The panel being changed, if it's an existing panel.</param>
        /// <param name="u">The lowest unit.</param>
        /// <param name="column">The left column.</param>
        /// <param name="width">The width in columns.</param>
        /// <param name="height">The height in units.</param>
        /// <returns>A message if the spot is not valid, otherwise null.</returns>
        private string? Check(int? deviceId, int? panelId, int u, int column, int width, int height)
        {
            if (width != 1 && width != 2 && width != 4)
            {
                return "Width must be Full, Half or Quarter.";
            }
            if (column < 0 || column + width > 4 || column % width != 0)
            {
                return "That column doesn't fit the width. Half width starts at Column 1 or 3, Full width at Column 1.";
            }
            if (u < 1 || height < 1 || u + height - 1 > Rack.HeightU)
            {
                return $"It must sit between unit 1 and unit {Rack.HeightU}.";
            }
            Device? clash = Placed.FirstOrDefault(other => other.Id != deviceId && u <= other.RackU!.Value + other.RackHeight - 1 && other.RackU!.Value <= u + height - 1 && column < other.RackColumn!.Value + other.RackWidth && other.RackColumn!.Value < column + width);
            if (clash != null)
            {
                return $"That spot is taken by {clash.Name}.";
            }
            RackPanel? panel = Panels.FirstOrDefault(other => other.Id != panelId && u <= other.RackU + other.RackHeight - 1 && other.RackU <= u + height - 1 && column < other.RackColumn + other.RackWidth && other.RackColumn < column + width);
            return panel == null ? null : $"That spot is taken by a {panel.Kind} Panel at unit {panel.RackU}.";
        }
    }
}