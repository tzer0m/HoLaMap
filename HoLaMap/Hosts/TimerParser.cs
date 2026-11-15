using System.Globalization;
using HoLaMap.Hosts.Models;
using HoLaMap.Schedules;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Reads a host's timer report: its time zone from timedatectl show, its timers from systemctl show, then the last run of each unit they start, also from systemctl show, all with unix timestamps.
    /// </summary>
    public static class TimerParser
    {
        /// <summary>
        /// Where units set up by hand live, as opposed to ones that came with the OS or a package.
        /// </summary>
        private const string LocalUnitPath = "/etc/systemd/system/";

        /// <summary>
        /// Gets the host's time zone, which its timers' calendar times are in.
        /// </summary>
        /// <param name="output">The report.</param>
        /// <returns>The time zone, like Europe/London, or null if the report doesn't say.</returns>
        public static string? TimeZone(string output)
        {
            return Blocks(output).Select(block => First(block, "Timezone")).FirstOrDefault(zone => !string.IsNullOrEmpty(zone));
        }

        /// <summary>
        /// Gets the timers set up by hand, plus any packaged ones asked for, leaving out ones with no repeating schedule, like those that only fire at boot.
        /// </summary>
        /// <param name="output">The report.</param>
        /// <param name="include">Packaged timers to keep as well, without the .timer suffix.</param>
        /// <returns>The timers.</returns>
        public static List<ReportedTimer> Timers(string output, List<string> include)
        {
            List<Dictionary<string, List<string>>> blocks = Blocks(output);
            Dictionary<string, Dictionary<string, List<string>>> units = blocks.Where(block => First(block, "Id") is string id && !id.EndsWith(".timer", StringComparison.Ordinal)).DistinctBy(block => First(block, "Id")).ToDictionary(block => First(block, "Id")!);
            List<ReportedTimer> timers = [];
            foreach (Dictionary<string, List<string>> block in blocks.Where(block => First(block, "Id")?.EndsWith(".timer", StringComparison.Ordinal) == true).DistinctBy(block => First(block, "Id")))
            {
                string name = First(block, "Id")![..^".timer".Length];
                bool custom = First(block, "FragmentPath")?.StartsWith(LocalUnitPath, StringComparison.Ordinal) == true;
                if (!custom && !include.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                string schedule = SystemdCalendar.ToCron(All(block, "TimersCalendar"), All(block, "TimersMonotonic"));
                if (schedule.Length == 0)
                {
                    continue;
                }
                string unit = First(block, "Unit") is string started && started.Length > 0 ? started : $"{name}.service";
                Dictionary<string, List<string>> run = units.GetValueOrDefault(unit) ?? [];
                DateTime? start = Time(run, "ExecMainStartTimestamp");
                DateTime? end = Time(run, "ExecMainExitTimestamp");
                // An exit before the last start belongs to an earlier run, so this one is still going.
                timers.Add(new ReportedTimer { Name = name, Unit = unit, Schedule = schedule, Active = First(block, "ActiveState") == "active", LastStart = start, LastEnd = end >= start ? end : null, Result = First(run, "Result") });
            }
            return [.. timers.OrderBy(timer => timer.Name)];
        }

        /// <summary>
        /// Splits the report into blocks of Key=Value lines, one per unit, where a key can appear more than once.
        /// </summary>
        /// <param name="output">The report.</param>
        /// <returns>The blocks.</returns>
        private static List<Dictionary<string, List<string>>> Blocks(string output)
        {
            List<Dictionary<string, List<string>>> blocks = [];
            foreach (string text in output.ReplaceLineEndings("\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
            {
                Dictionary<string, List<string>> values = [];
                foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    int equals = line.IndexOf('=');
                    if (equals > 0)
                    {
                        string key = line[..equals];
                        if (!values.TryGetValue(key, out List<string>? list))
                        {
                            list = [];
                            values[key] = list;
                        }
                        list.Add(line[(equals + 1)..]);
                    }
                }
                if (values.Count > 0)
                {
                    blocks.Add(values);
                }
            }
            return blocks;
        }

        /// <summary>
        /// Gets the first value of a key in a block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value, or null if the key isn't there.</returns>
        private static string? First(Dictionary<string, List<string>> block, string key) => block.TryGetValue(key, out List<string>? values) ? values[0] : null;

        /// <summary>
        /// Gets every non-empty value of a key in a block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="key">The key.</param>
        /// <returns>The values.</returns>
        private static List<string> All(Dictionary<string, List<string>> block, string key) => block.TryGetValue(key, out List<string>? values) ? [.. values.Where(value => value.Length > 0)] : [];

        /// <summary>
        /// Reads a unix timestamp written as @seconds.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="key">The key.</param>
        /// <returns>The time (UTC), or null if it's missing, empty or zero.</returns>
        private static DateTime? Time(Dictionary<string, List<string>> block, string key)
        {
            string text = First(block, key)?.TrimStart('@') ?? string.Empty;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) && seconds > 0 ? DateTime.UnixEpoch.AddSeconds(seconds) : null;
        }
    }
}