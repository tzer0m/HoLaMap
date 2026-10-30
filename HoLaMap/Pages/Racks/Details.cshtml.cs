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
        /// The tab asked for: device, panel, patch or rack.
        /// </summary>
        [BindProperty(SupportsGet = true, Name = "tab")]
        public string? Tab { get; set; }

        /// <summary>
        /// The tab to show, falling back to the one that fits what's selected.
        /// </summary>
        public string ActiveTab => Tab is "device" or "panel" or "rack" || (Tab == "patch" && EditingPanel?.Kind == RackPanelKind.Patch) ? Tab : EditingPanel == null ? "device" : EditingPanel.Kind == RackPanelKind.Patch ? "patch" : "panel";

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
        /// The devices and ports a patch panel port can be patched to.
        /// </summary>
        public List<SelectListItem> PatchTargets { get; private set; } = [];

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
        /// <param name="name">The name shown for a patch panel.</param>
        /// <param name="ports">How many ports a patch panel has: 12, 24 or 48.</param>
        /// <param name="u">The lowest unit the panel occupies.</param>
        /// <param name="column">The left column from 0 to 3.</param>
        /// <param name="width">The width in columns: 1, 2 or 4.</param>
        /// <param name="height">The height in units.</param>
        /// <returns>A redirect back to the rack, or the page again if the spot is not valid.</returns>
        public async Task<IActionResult> OnPostPlacePanelAsync(int id, int? panelId, RackPanelKind kind, string? name, int ports, int u, int column, int width, int height)
        {
            if (!await LoadAsync(id))
            {
                return NotFound();
            }
            RackPanel? panel = panelId == null ? new RackPanel { RackId = id } : await context.RackPanels.Include(item => item.Connections).FirstOrDefaultAsync(item => item.Id == panelId && item.RackId == id);
            if (panel == null)
            {
                return RedirectToPage(new { id });
            }
            string? problem = kind == RackPanelKind.Patch && ports != 12 && ports != 24 && ports != 48 ? "A patch panel has 12, 24 or 48 ports." : Check(null, panelId, u, column, width, height);
            if (problem != null)
            {
                Error = problem;
                Tab = "panel";
                EditPanelId = panelId;
                EditingPanel = Panels.FirstOrDefault(item => item.Id == panelId);
                return Page();
            }
            panel.Kind = kind;
            panel.Name = kind == RackPanelKind.Patch && !string.IsNullOrWhiteSpace(name) ? name.Trim() : null;
            panel.PortCount = kind == RackPanelKind.Patch ? ports : 0;
            context.PatchConnections.RemoveRange(panel.Connections.Where(connection => connection.Number > panel.PortCount));
            panel.RackU = u;
            panel.RackColumn = column;
            panel.RackWidth = width;
            panel.RackHeight = height;
            if (panelId == null)
            {
                context.RackPanels.Add(panel);
            }
            await context.SaveChangesAsync();
            return kind == RackPanelKind.Patch ? RedirectToPage(new { id, panel = panel.Id, tab = "patch" }) : RedirectToPage(new { id, tab = "panel" });
        }

        /// <summary>
        /// Plugs a cable into one side of a patch panel port, replacing any cable already there.
        /// </summary>
        /// <param name="id">The rack ID.</param>
        /// <param name="panelId">The patch panel.</param>
        /// <param name="number">The patch panel port number.</param>
        /// <param name="side">Which side of the port.</param>
        /// <param name="target">The other end, as device:ID or port:ID.</param>
        /// <returns>A redirect back to the patch panel.</returns>
        public async Task<IActionResult> OnPostPatchAsync(int id, int panelId, int number, PatchSide side, string? target)
        {
            RackPanel? panel = await context.RackPanels.Include(item => item.Connections).FirstOrDefaultAsync(item => item.Id == panelId && item.RackId == id && item.Kind == RackPanelKind.Patch);
            string[] parts = (target ?? string.Empty).Split(':');
            if (panel == null || number < 1 || number > panel.PortCount || parts.Length != 2 || !int.TryParse(parts[1], out int targetId))
            {
                return RedirectToPage(new { id, panel = panelId });
            }
            Port? port = parts[0] == "port" ? await context.Ports.FirstOrDefaultAsync(item => item.Id == targetId) : null;
            int? deviceId = port?.DeviceId ?? (parts[0] == "device" && await context.Devices.AnyAsync(item => item.Id == targetId) ? targetId : null);
            if (deviceId == null)
            {
                return RedirectToPage(new { id, panel = panelId });
            }
            PatchConnection? connection = panel.Connections.FirstOrDefault(item => item.Number == number && item.Side == side);
            if (connection == null)
            {
                connection = new PatchConnection { RackPanelId = panelId, Number = number, Side = side };
                context.PatchConnections.Add(connection);
            }
            connection.DeviceId = deviceId.Value;
            connection.PortId = port?.Id;
            await context.SaveChangesAsync();
            return RedirectToPage(new { id, panel = panelId });
        }

        /// <summary>
        /// Unplugs a cable from a patch panel port.
        /// </summary>
        /// <param name="id">The rack ID.</param>
        /// <param name="panelId">The patch panel.</param>
        /// <param name="connectionId">The cable to unplug.</param>
        /// <returns>A redirect back to the patch panel.</returns>
        public async Task<IActionResult> OnPostUnpatchAsync(int id, int panelId, int connectionId)
        {
            PatchConnection? connection = await context.PatchConnections.FirstOrDefaultAsync(item => item.Id == connectionId && item.RackPanelId == panelId && item.RackPanel.RackId == id);
            if (connection != null)
            {
                context.PatchConnections.Remove(connection);
                await context.SaveChangesAsync();
            }
            return RedirectToPage(new { id, panel = panelId });
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
                Tab = "rack";
                return Page();
            }
            if (heightU < Math.Max(1, highest))
            {
                Error = $"Devices or panels reach unit {highest}, so the rack can't be shorter than that.";
                Tab = "rack";
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
            List<RackPanel> panels = await context.RackPanels.Where(panel => panel.RackId == id).Include(panel => panel.Connections).ThenInclude(connection => connection.Device).Include(panel => panel.Connections).ThenInclude(connection => connection.Port).ToListAsync();
            Panels = [.. panels.OrderByDescending(panel => panel.RackU).ThenBy(panel => panel.RackColumn)];
            EditingPanel = Panels.FirstOrDefault(panel => panel.Id == EditPanelId);
            if (EditingPanel?.Kind == RackPanelKind.Patch)
            {
                List<Device> targets = await context.Devices.Include(device => device.Ports).ToListAsync();
                foreach (Device device in targets.OrderBy(device => device.Kind).ThenBy(device => device.Name))
                {
                    SelectListGroup group = new() { Name = device.Name };
                    PatchTargets.Add(new SelectListItem($"{device.Name} (No Port)", $"device:{device.Id}") { Group = group });
                    PatchTargets.AddRange(device.Ports.OrderBy(port => port.Index).Select(port => new SelectListItem(string.IsNullOrEmpty(port.Name) ? $"Port {port.Index}" : $"Port {port.Index} · {port.Name}", $"port:{port.Id}") { Group = group }));
                }
            }
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
            return panel == null ? null : $"That spot is taken by {panel.Name ?? $"a {panel.Kind} Panel"} at unit {panel.RackU}.";
        }
    }
}