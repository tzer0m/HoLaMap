using System.Globalization;
using System.Text.RegularExpressions;

namespace HoLaMap.Schedules
{
    /// <summary>
    /// Turns a systemd timer's schedule into a cron expression, so it can be drawn with everything else. Calendar times like Mon..Fri *-*-* 09:30:00 convert exactly; repeating intervals like every 5 minutes convert to the nearest cron, since their start depends on when the timer began.
    /// </summary>
    public static partial class SystemdCalendar
    {
        /// <summary>
        /// The days of the week as systemd shortens them, from Sunday, so their index is cron's number.
        /// </summary>
        private static readonly string[] Weekdays = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

        /// <summary>
        /// Minutes in each unit systemd writes a time span in.
        /// </summary>
        private static readonly Dictionary<string, double> Units = new(StringComparer.OrdinalIgnoreCase) { ["us"] = 1 / 60e6, ["ms"] = 1 / 60e3, ["s"] = 1 / 60.0, ["sec"] = 1 / 60.0, ["m"] = 1, ["min"] = 1, ["h"] = 60, ["hr"] = 60, ["d"] = 1440, ["w"] = 10080 };

        /// <summary>
        /// Converts a timer's schedule, from the TimersCalendar and TimersMonotonic values systemctl show gives.
        /// </summary>
        /// <param name="calendar">The TimersCalendar values, like { OnCalendar=*-*-* 03:00:00 ; next_elapse=... }.</param>
        /// <param name="monotonic">The TimersMonotonic values, like { OnUnitActiveUSec=5min ; next_elapse=... }.</param>
        /// <returns>The cron expression, or the schedule as systemd wrote it if it can't be converted, or an empty string if it has none.</returns>
        public static string ToCron(List<string> calendar, List<string> monotonic)
        {
            string? spec = calendar.Select(value => Setting(value, "OnCalendar")).FirstOrDefault(value => value != null);
            if (spec != null)
            {
                return FromCalendar(spec) ?? $"OnCalendar={spec}";
            }
            // Only intervals counted from the unit's last run repeat; ones counted from boot or start-up fire once.
            string? every = monotonic.Select(value => Setting(value, "OnUnitActiveUSec") ?? Setting(value, "OnUnitInactiveUSec")).FirstOrDefault(value => value != null);
            if (every != null)
            {
                return FromInterval(every) ?? $"Every {every}";
            }
            return string.Empty;
        }

        /// <summary>
        /// Reads one setting out of a value like { OnCalendar=*-*-* 03:00:00 ; next_elapse=... }.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="name">The setting's name.</param>
        /// <returns>The setting, or null if it isn't there.</returns>
        private static string? Setting(string value, string name)
        {
            foreach (string part in value.Trim().TrimStart('{').TrimEnd('}').Split(';'))
            {
                string text = part.Trim();
                if (text.StartsWith($"{name}=", StringComparison.Ordinal))
                {
                    return text[(name.Length + 1)..].Trim();
                }
            }
            return null;
        }

        /// <summary>
        /// Converts a calendar time as systemd normalises it, an optional day of the week, then year-month-day and hour:minute:second.
        /// </summary>
        /// <param name="spec">The calendar time, like Mon..Fri *-*-* 09:30:00.</param>
        /// <returns>The cron expression, or null if it uses something cron can't say, like a given year or the last day of the month.</returns>
        private static string? FromCalendar(string spec)
        {
            List<string> parts = [.. spec.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
            string weekdays = "*";
            if (parts.Count > 0 && char.IsLetter(parts[0][0]))
            {
                string? days = Days(parts[0]);
                if (days == null)
                {
                    return null;
                }
                weekdays = days;
                parts.RemoveAt(0);
            }
            if (parts.Count != 2)
            {
                return null;
            }
            string[] date = parts[0].Split('-');
            string[] time = parts[1].Split(':');
            if (date.Length != 3 || date[0] != "*" || time.Length != 3)
            {
                return null;
            }
            string?[] fields = [Field(time[1]), Field(time[0]), Field(date[2]), Field(date[1])];
            return fields.Any(field => field == null) ? null : $"{string.Join(' ', fields)} {weekdays}";
        }

        /// <summary>
        /// Converts one part of a calendar time, where systemd writes ranges with .. and pads numbers with zeros.
        /// </summary>
        /// <param name="text">The part, like 00/5 or 1..5.</param>
        /// <returns>The cron field, or null if it uses something cron can't say.</returns>
        private static string? Field(string text)
        {
            string field = LeadingZeros().Replace(text.Replace("..", "-"), "$1");
            return CronCharacters().IsMatch(field) ? field : null;
        }

        /// <summary>
        /// Converts the days of the week, like Mon..Fri or Sat,Sun, to cron's numbers.
        /// </summary>
        /// <param name="text">The days.</param>
        /// <returns>The cron field, or null if a day isn't recognised or a range wraps round the week.</returns>
        private static string? Days(string text)
        {
            List<string> items = [];
            foreach (string item in text.Split(','))
            {
                int[] ends = [.. item.Split("..").Select(name => Array.FindIndex(Weekdays, day => name.StartsWith(day, StringComparison.OrdinalIgnoreCase)))];
                // Sunday ends a range in systemd's Monday-first week, so it counts as 7 there.
                if (ends.Length == 2 && ends[1] == 0)
                {
                    ends[1] = 7;
                }
                if (ends.Any(end => end < 0) || ends.Length > 2 || (ends.Length == 2 && ends[0] > ends[1]))
                {
                    return null;
                }
                items.Add(string.Join('-', ends));
            }
            return string.Join(',', items);
        }

        /// <summary>
        /// Converts a repeating interval to the cron expression that runs as often, if the interval divides evenly into an hour or a day.
        /// </summary>
        /// <param name="span">The interval as systemd writes it, like 5min or 1h 30min.</param>
        /// <returns>The cron expression, or null if no cron runs that often.</returns>
        private static string? FromInterval(string span)
        {
            double total = 0;
            foreach (Match match in SpanPart().Matches(span))
            {
                if (!Units.TryGetValue(match.Groups[2].Value, out double minutes))
                {
                    return null;
                }
                total += double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) * minutes;
            }
            int every = (int)Math.Round(total);
            return every switch
            {
                1 => "* * * * *",
                > 1 and < 60 when 60 % every == 0 => $"*/{every} * * * *",
                60 => "0 * * * *",
                > 60 and < 1440 when every % 60 == 0 && 24 % (every / 60) == 0 => $"0 */{every / 60} * * *",
                1440 => "0 0 * * *",
                10080 => "0 0 * * 1",
                _ => null
            };
        }

        /// <summary>
        /// Matches zeros padding the front of a number.
        /// </summary>
        /// <returns>The pattern.</returns>
        [GeneratedRegex(@"\b0+(\d)")]
        private static partial Regex LeadingZeros();

        /// <summary>
        /// Matches a field made only of what cron allows.
        /// </summary>
        /// <returns>The pattern.</returns>
        [GeneratedRegex(@"^[0-9*,/-]+$")]
        private static partial Regex CronCharacters();

        /// <summary>
        /// Matches one number and unit of a time span, like 30min.
        /// </summary>
        /// <returns>The pattern.</returns>
        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*([a-z]+)", RegexOptions.IgnoreCase)]
        private static partial Regex SpanPart();
    }
}