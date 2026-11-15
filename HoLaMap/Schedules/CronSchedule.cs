using System.Globalization;

namespace HoLaMap.Schedules
{
    /// <summary>
    /// A five-field cron expression (minute, hour, day of month, month, day of week), read so it can be matched against times and described in words.
    /// </summary>
    /// <param name="fields">The five fields as written.</param>
    /// <param name="minutes">The minutes it runs at.</param>
    /// <param name="hours">The hours it runs at.</param>
    /// <param name="days">The days of the month it runs on.</param>
    /// <param name="months">The months it runs in.</param>
    /// <param name="weekdays">The days of the week it runs on, Sunday being 0.</param>
    public class CronSchedule(string[] fields, SortedSet<int> minutes, SortedSet<int> hours, SortedSet<int> days, SortedSet<int> months, SortedSet<int> weekdays)
    {
        /// <summary>
        /// Short names for the months, used in expressions and descriptions.
        /// </summary>
        private static readonly string[] MonthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        /// <summary>
        /// Short names for the days of the week from Sunday, used in expressions and descriptions.
        /// </summary>
        private static readonly string[] WeekdayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

        /// <summary>
        /// The shorthand expressions cron accepts and what they stand for.
        /// </summary>
        private static readonly Dictionary<string, string> Macros = new(StringComparer.OrdinalIgnoreCase) { ["@yearly"] = "0 0 1 1 *", ["@annually"] = "0 0 1 1 *", ["@monthly"] = "0 0 1 * *", ["@weekly"] = "0 0 * * 0", ["@daily"] = "0 0 * * *", ["@midnight"] = "0 0 * * *", ["@hourly"] = "0 * * * *" };

        /// <summary>
        /// Gets whether the day of month is limited.
        /// </summary>
        private bool DayLimited => fields[2] != "*";

        /// <summary>
        /// Gets whether the day of week is limited.
        /// </summary>
        private bool WeekdayLimited => fields[4] != "*";

        /// <summary>
        /// Gets the bar it belongs on: the month bar if it picks days of the month or months, the week bar if it picks days of the week, otherwise the day bar.
        /// </summary>
        public SchedulePeriod Period => DayLimited || fields[3] != "*" ? SchedulePeriod.Month : WeekdayLimited ? SchedulePeriod.Week : SchedulePeriod.Day;

        /// <summary>
        /// Reads an expression.
        /// </summary>
        /// <param name="expression">The expression, five fields or a shorthand like @daily.</param>
        /// <returns>The schedule, or null if it can't be read.</returns>
        public static CronSchedule? Parse(string expression)
        {
            string text = Macros.TryGetValue(expression.Trim(), out string? expanded) ? expanded : expression;
            string[] fields = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (fields.Length != 5)
            {
                return null;
            }
            SortedSet<int>? minutes = Field(fields[0], 0, 59, null);
            SortedSet<int>? hours = Field(fields[1], 0, 23, null);
            SortedSet<int>? days = Field(fields[2], 1, 31, null);
            SortedSet<int>? months = Field(fields[3], 1, 12, MonthNames);
            SortedSet<int>? weekdays = Field(fields[4], 0, 7, WeekdayNames);
            if (minutes == null || hours == null || days == null || months == null || weekdays == null)
            {
                return null;
            }
            // Sunday can be written as 7 as well as 0.
            if (weekdays.Remove(7))
            {
                weekdays.Add(0);
            }
            return new CronSchedule(fields, minutes, hours, days, months, weekdays);
        }

        /// <summary>
        /// Checks whether it runs at a time. When both day fields are limited, either one matching is enough, as in cron.
        /// </summary>
        /// <param name="time">The time, in the schedule's own time zone.</param>
        /// <returns>Whether it runs then.</returns>
        public bool Matches(DateTime time)
        {
            if (!minutes.Contains(time.Minute) || !hours.Contains(time.Hour) || !months.Contains(time.Month))
            {
                return false;
            }
            bool day = days.Contains(time.Day);
            bool weekday = weekdays.Contains((int)time.DayOfWeek);
            return DayLimited && WeekdayLimited ? day || weekday : day && weekday;
        }

        /// <summary>
        /// Lists every time it runs between two times.
        /// </summary>
        /// <param name="fromUtc">The start (UTC), included.</param>
        /// <param name="toUtc">The end (UTC), left out.</param>
        /// <param name="zone">The time zone the schedule's times are in.</param>
        /// <returns>The times it runs (UTC).</returns>
        public IEnumerable<DateTime> Occurrences(DateTime fromUtc, DateTime toUtc, TimeZoneInfo zone)
        {
            DateTime minute = new(fromUtc.Year, fromUtc.Month, fromUtc.Day, fromUtc.Hour, fromUtc.Minute, 0, DateTimeKind.Utc);
            if (minute < fromUtc)
            {
                minute = minute.AddMinutes(1);
            }
            for (; minute < toUtc; minute = minute.AddMinutes(1))
            {
                if (Matches(TimeZoneInfo.ConvertTimeFromUtc(minute, zone)))
                {
                    yield return minute;
                }
            }
        }

        /// <summary>
        /// Describes it in words, like "Daily at 02:00", "Every 5 minutes" or "Mon to Fri at 09:30".
        /// </summary>
        /// <returns>The description, or the expression itself if it's too unusual to put into words.</returns>
        public string Describe()
        {
            string? time = DescribeTime();
            if (time == null)
            {
                return $"cron {string.Join(' ', fields)}";
            }
            List<string> parts = [];
            if (DayLimited)
            {
                parts.Add(days.Count == 1 ? $"On day {days.Min} of the month" : $"On days {string.Join(", ", days)} of the month");
            }
            if (WeekdayLimited)
            {
                parts.Add(DayLimited ? $"or every {Weekdays()}" : $"Every {Weekdays()}");
            }
            if (fields[3] != "*")
            {
                parts.Add($"in {string.Join(", ", months.Select(month => MonthNames[month - 1]))}");
            }
            bool frequent = fields[1] == "*" || fields[1].StartsWith("*/", StringComparison.Ordinal);
            if (parts.Count == 0)
            {
                parts.Add(frequent ? string.Empty : "Daily");
            }
            string when = string.Join(' ', parts.Where(part => part.Length > 0));
            return when.Length == 0 ? char.ToUpperInvariant(time[0]) + time[1..] : $"{when} {(frequent ? time : $"at {time}")}";
        }

        /// <summary>
        /// Describes the time of day part, like "02:00", "every hour" or "every 5 minutes".
        /// </summary>
        /// <returns>The words, or null if the minute and hour fields are too unusual.</returns>
        private string? DescribeTime()
        {
            string minute = fields[0];
            string hour = fields[1];
            if (hour == "*" && minute == "*")
            {
                return "every minute";
            }
            if (hour == "*" && Step(minute) is int every)
            {
                return $"every {every} minutes";
            }
            if (minutes.Count != 1)
            {
                return null;
            }
            int at = minutes.Min;
            if (hour == "*")
            {
                return at == 0 ? "every hour" : $"every hour at {at} past";
            }
            if (Step(hour) is int hourly)
            {
                return $"every {hourly} hours at :{at:00}";
            }
            return string.Join(", ", hours.Select(item => $"{item:00}:{at:00}"));
        }

        /// <summary>
        /// Lists the days of the week, using a range for three or more in a row, like "Mon to Fri".
        /// </summary>
        /// <returns>The days.</returns>
        private string Weekdays()
        {
            // Monday first, so a working week reads as one range.
            List<int> order = [.. weekdays.Select(day => (day + 6) % 7).Order()];
            bool run = order.Count >= 3 && order[^1] - order[0] == order.Count - 1;
            string Name(int index) => WeekdayNames[(index + 1) % 7];
            return run ? $"{Name(order[0])} to {Name(order[^1])}" : string.Join(", ", order.Select(Name));
        }

        /// <summary>
        /// Reads a step like */5.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns>The step, or null if the field isn't one.</returns>
        private static int? Step(string field)
        {
            return field.StartsWith("*/", StringComparison.Ordinal) && int.TryParse(field[2..], NumberStyles.None, CultureInfo.InvariantCulture, out int step) && step > 0 ? step : null;
        }

        /// <summary>
        /// Reads one field, made of comma-separated values, ranges like 1-5 and steps like */15 or 0-30/10.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="min">The lowest value allowed.</param>
        /// <param name="max">The highest value allowed.</param>
        /// <param name="names">Names that can stand in for values, the first being min, or null if there are none.</param>
        /// <returns>The values, or null if the field can't be read.</returns>
        private static SortedSet<int>? Field(string field, int min, int max, string[]? names)
        {
            SortedSet<int> values = [];
            foreach (string part in field.Split(','))
            {
                string[] stepped = part.Split('/');
                int step = 1;
                if (stepped.Length > 2 || (stepped.Length == 2 && (!int.TryParse(stepped[1], NumberStyles.None, CultureInfo.InvariantCulture, out step) || step < 1)))
                {
                    return null;
                }
                int from;
                int to;
                if (stepped[0] == "*")
                {
                    from = min;
                    to = max;
                }
                else
                {
                    string[] range = stepped[0].Split('-');
                    int? start = range.Length is 1 or 2 ? Value(range[0], min, names) : null;
                    int? end = range.Length == 2 ? Value(range[1], min, names) : stepped.Length == 2 ? max : start;
                    if (start == null || end == null || start < min || end > max || start > end)
                    {
                        return null;
                    }
                    from = start.Value;
                    to = end.Value;
                }
                for (int value = from; value <= to; value += step)
                {
                    values.Add(value);
                }
            }
            return values;
        }

        /// <summary>
        /// Reads a single value, as a number or a name like Mon or Jan.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="min">The value the first name stands for.</param>
        /// <param name="names">Names that can stand in for values, or null if there are none.</param>
        /// <returns>The value, or null if it can't be read.</returns>
        private static int? Value(string text, int min, string[]? names)
        {
            if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
            {
                return number;
            }
            int index = names == null ? -1 : Array.FindIndex(names, name => name.Equals(text, StringComparison.OrdinalIgnoreCase));
            return index < 0 ? null : min + index;
        }
    }
}