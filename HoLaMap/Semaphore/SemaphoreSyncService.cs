using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HoLaMap.Semaphore
{
    /// <summary>
    /// Reads Semaphore's schedules, the templates they run and how long recent runs took.
    /// </summary>
    /// <param name="http">The HTTP client.</param>
    /// <param name="context">The database context.</param>
    /// <param name="options">The Semaphore settings.</param>
    public class SemaphoreSyncService(HttpClient http, HoLaMapContext context, IOptions<SemaphoreOptions> options)
    {
        /// <summary>
        /// Updates the Semaphore tasks to match its schedules. A schedule no longer there is removed.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of schedules.</returns>
        public async Task<int> SyncAsync(CancellationToken cancellationToken)
        {
            string project = $"project/{options.Value.ProjectId}";
            using JsonDocument schedules = await GetAsync($"{project}/schedules", cancellationToken);
            using JsonDocument templates = await GetAsync($"{project}/templates", cancellationToken);
            using JsonDocument inventories = await GetAsync($"{project}/inventory", cancellationToken);
            using JsonDocument runs = await GetAsync($"{project}/tasks/last", cancellationToken);
            Dictionary<int, JsonElement> templatesById = templates.RootElement.EnumerateArray().ToDictionary(item => Number(item, "id"));
            Dictionary<int, JsonElement> inventoriesById = inventories.RootElement.EnumerateArray().ToDictionary(item => Number(item, "id"));
            ILookup<int, JsonElement> runsByTemplate = runs.RootElement.EnumerateArray().OrderByDescending(item => Time(item, "created")).ToLookup(item => Number(item, "template_id"));
            List<Device> devices = await context.Devices.ToListAsync(cancellationToken);
            List<ScheduledTask> tasks = await context.ScheduledTasks.Where(task => task.Source == ScheduleSource.Semaphore).ToListAsync(cancellationToken);
            HashSet<string> seen = [];
            DateTime now = DateTime.UtcNow;
            foreach (JsonElement schedule in schedules.RootElement.EnumerateArray().Where(item => Text(item, "cron_format").Length > 0))
            {
                string id = Number(schedule, "id").ToString(CultureInfo.InvariantCulture);
                seen.Add(id);
                ScheduledTask? task = tasks.FirstOrDefault(existing => existing.ExternalId == id);
                if (task == null)
                {
                    task = new ScheduledTask { Source = ScheduleSource.Semaphore, ExternalId = id };
                    context.ScheduledTasks.Add(task);
                }
                int templateId = Number(schedule, "template_id");
                bool hasTemplate = templatesById.TryGetValue(templateId, out JsonElement template);
                JsonElement inventory = default;
                bool hasInventory = hasTemplate && inventoriesById.TryGetValue(Number(template, "inventory_id"), out inventory);
                string name = Text(schedule, "name");
                task.Name = name.Length > 0 ? name : Text(schedule, "tpl_name");
                task.Schedule = Text(schedule, "cron_format");
                task.TimeZone = options.Value.TimeZone;
                task.Active = schedule.TryGetProperty("active", out JsonElement active) && active.ValueKind == JsonValueKind.True;
                task.Command = hasTemplate ? Text(template, "playbook") : string.Empty;
                task.Target = hasInventory ? Text(inventory, "name") : string.Empty;
                task.DeviceId = hasInventory ? DeviceOf(inventory, devices)?.Id : null;
                // Runs come newest first, so the first is the last run, and durations are only taken from runs that finished well.
                List<JsonElement> history = [.. runsByTemplate[templateId]];
                task.LastRun = history.Count > 0 ? Time(history[0], "start") ?? Time(history[0], "created") : null;
                task.LastStatus = history.Count > 0 ? Text(history[0], "status") : null;
                task.Durations = [.. history.Where(run => Text(run, "status") == "success").Select(Duration).OfType<int>().Take(options.Value.DurationCount)];
                task.LastSeen = now;
            }
            context.ScheduledTasks.RemoveRange(tasks.Where(task => !seen.Contains(task.ExternalId)));
            await context.SaveChangesAsync(cancellationToken);
            return seen.Count;
        }

        /// <summary>
        /// Finds the device an inventory runs against, by its name or by the folder its file is in, like inventory/tyrion/hosts.yml.
        /// </summary>
        /// <param name="inventory">The inventory.</param>
        /// <param name="devices">Every device.</param>
        /// <returns>The device, or null if none matches.</returns>
        private static Device? DeviceOf(JsonElement inventory, List<Device> devices)
        {
            string name = Text(inventory, "name");
            string[] parts = Text(inventory, "inventory").Split('/', StringSplitOptions.RemoveEmptyEntries);
            string folder = parts.Length >= 2 ? parts[^2] : string.Empty;
            return devices.FirstOrDefault(device => device.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? devices.FirstOrDefault(device => folder.Length > 0 && device.Name.Equals(folder, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Works out how long a finished run took.
        /// </summary>
        /// <param name="run">The run.</param>
        /// <returns>The seconds it took, or null if it hasn't both started and ended.</returns>
        private static int? Duration(JsonElement run)
        {
            DateTime? start = Time(run, "start");
            DateTime? end = Time(run, "end");
            return start != null && end != null && end >= start ? (int)Math.Round((end.Value - start.Value).TotalSeconds) : null;
        }

        /// <summary>
        /// Calls Semaphore's API with the token.
        /// </summary>
        /// <param name="path">The path after /api/.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response.</returns>
        private async Task<JsonDocument> GetAsync(string path, CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"{options.Value.BaseUrl.TrimEnd('/')}/api/{path}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiToken);
            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }

        /// <summary>
        /// Gets a string property, or an empty string if it's missing or not a string.
        /// </summary>
        /// <param name="element">The JSON object.</param>
        /// <param name="name">The property name.</param>
        /// <returns>The value.</returns>
        private static string Text(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Gets a whole number property, or 0 if it's missing or not a number.
        /// </summary>
        /// <param name="element">The JSON object.</param>
        /// <param name="name">The property name.</param>
        /// <returns>The value.</returns>
        private static int Number(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) ? number : 0;
        }

        /// <summary>
        /// Gets a timestamp property as UTC, or null if it's missing, not a date or Go's empty date.
        /// </summary>
        /// <param name="element">The JSON object.</param>
        /// <param name="name">The property name.</param>
        /// <returns>The time, or null.</returns>
        private static DateTime? Time(JsonElement element, string name)
        {
            return DateTime.TryParse(Text(element, name), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime time) && time.Year > 1 ? time : null;
        }
    }
}