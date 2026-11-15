using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using HoLaMap.Data;
using HoLaMap.Models;
using HoLaMap.Schedules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Pages.Schedule
{
    /// <summary>
    /// Shows everything scheduled across the fleet on four bars, an hour, a day, a week and a month, so clashes and quiet spells stand out.
    /// </summary>
    /// <param name="context">The database context.</param>
    public class IndexModel(HoLaMapContext context) : PageModel
    {
        /// <summary>
        /// The colours given to hosts in name order, repeating if there are more hosts than colours.
        /// </summary>
        private static readonly string[] Palette = ["#4e9af1", "#f2a03d", "#63c27a", "#e0629a", "#a77de8", "#3fc4c4", "#e5d34a", "#ef6b5b", "#8fb3ff", "#b5d86a"];

        /// <summary>
        /// Gets or sets the host to show on its own, from the query string, or null to show every host.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? Host { get; set; }

        /// <summary>
        /// Gets or sets the source to show on its own, from the query string, or null to show every source.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public ScheduleSource? Source { get; set; }

        /// <summary>
        /// The sources tasks come from, in order, for the key.
        /// </summary>
        public List<ScheduleSource> Sources { get; private set; } = [];

        /// <summary>
        /// The hour, day, week and month bars.
        /// </summary>
        public List<ScheduleBar> Bars { get; private set; } = [];

        /// <summary>
        /// Each host's colour, in name order. Every host is listed, even when only one is shown, so the key can switch between them.
        /// </summary>
        public Dictionary<string, string> Colours { get; private set; } = [];

        /// <summary>
        /// Tasks whose schedule couldn't be read, so they're on no bar.
        /// </summary>
        public List<ScheduledTask> Unread { get; private set; } = [];

        /// <summary>
        /// Every task's details as JSON, for the details pane.
        /// </summary>
        public string Details { get; private set; } = "[]";

        /// <summary>
        /// Gets whether there are any tasks at all.
        /// </summary>
        public bool Any { get; private set; }

        /// <summary>
        /// Lays out the tasks on the bars, only those for the chosen host and source if either is picked.
        /// </summary>
        /// <returns>A task that completes when the page has loaded.</returns>
        public async Task OnGetAsync()
        {
            List<ScheduledTask> all = await context.ScheduledTasks.Include(task => task.Device).OrderBy(task => task.Name).ToListAsync();
            Any = all.Count > 0;
            // Colours come from every host, so a host keeps its colour when it's shown on its own.
            List<string> hosts = [.. all.Select(ScheduleLayout.HostOf).Distinct().Order()];
            if (Host != null && !hosts.Contains(Host))
            {
                Host = null;
            }
            Sources = [.. all.Select(task => task.Source).Distinct().Order()];
            if (Source != null && !Sources.Contains(Source.Value))
            {
                Source = null;
            }
            List<ScheduledTask> tasks = [.. all.Where(task => (Host == null || ScheduleLayout.HostOf(task) == Host) && (Source == null || task.Source == Source))];
            TimeZoneInfo zone = TimeZoneInfo.Local;
            Bars = ScheduleLayout.Build(tasks, DateTime.UtcNow, zone);
            Colours = hosts.Select((host, index) => (host, index)).ToDictionary(item => item.host, item => Palette[item.index % Palette.Length]);
            Unread = [.. tasks.Where(task => CronSchedule.Parse(task.Schedule) == null)];
            Details = JsonSerializer.Serialize(tasks.Select(task => Describe(task, zone)), JsonSerializerOptions.Web);
        }

        /// <summary>
        /// Gets a source's name as shown on the page.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>The name, like Systemd Timer.</returns>
        public static string Label(ScheduleSource source) => typeof(ScheduleSource).GetField(source.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? source.ToString();

        /// <summary>
        /// Writes a share of a bar as a CSS percentage.
        /// </summary>
        /// <param name="share">The share, from 0 to 1.</param>
        /// <returns>The percentage, like 12.5%.</returns>
        public static string Percent(double share) => $"{(share * 100).ToString("0.###", CultureInfo.InvariantCulture)}%";

        /// <summary>
        /// Writes an SVG path with a short level mark for each run, on a strip 10 wide and 1000 high.
        /// </summary>
        /// <param name="positions">How far down the bar each run is, from 0 to 1.</param>
        /// <returns>The path data.</returns>
        public static string Marks(List<double> positions) => string.Concat(positions.Select(position => $"M0 {(position * 1000).ToString("0.#", CultureInfo.InvariantCulture)}h10"));

        /// <summary>
        /// Writes a number of seconds as a short duration, like 45s, 3m 20s or 1h 05m.
        /// </summary>
        /// <param name="seconds">The seconds.</param>
        /// <returns>The duration.</returns>
        public static string Duration(int seconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(seconds);
            return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes:00}m" : span.TotalMinutes >= 1 ? $"{span.Minutes}m {span.Seconds:00}s" : $"{span.Seconds}s";
        }

        /// <summary>
        /// Gathers what the details pane shows about a task.
        /// </summary>
        /// <param name="task">The task, with its device loaded.</param>
        /// <param name="zone">The time zone times are shown in.</param>
        /// <returns>The details.</returns>
        private static object Describe(ScheduledTask task, TimeZoneInfo zone)
        {
            CronSchedule? cron = CronSchedule.Parse(task.Schedule);
            TimeZoneInfo own = ScheduleLayout.ZoneOf(task);
            // Say which zone the schedule is written in when it isn't the one the page is drawn in, since the times in words are the schedule's own.
            string words = cron == null ? task.Schedule : cron.Describe();
            string schedule = own.HasSameRules(zone) ? words : $"{words} {task.TimeZone}";
            int? typical = ScheduleLayout.Typical(task);
            return new
            {
                task.Id,
                task.Name,
                Source = Label(task.Source),
                Host = ScheduleLayout.HostOf(task),
                task.Target,
                task.Command,
                Schedule = schedule,
                Cron = task.Schedule,
                task.Active,
                LastRun = task.LastRun == null ? string.Empty : TimeZoneInfo.ConvertTimeFromUtc(task.LastRun.Value, zone).ToString("ddd d MMM HH:mm"),
                LastStatus = task.LastStatus ?? string.Empty,
                Typical = typical == null ? string.Empty : Duration(typical.Value),
                Durations = task.Durations.Select(Duration)
            };
        }
    }
}