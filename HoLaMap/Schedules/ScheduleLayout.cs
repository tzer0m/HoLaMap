using HoLaMap.Models;

namespace HoLaMap.Schedules
{
    /// <summary>
    /// Lays the scheduled tasks out on the four bars: this hour, the day noon to noon, this week Monday to Sunday and this month 1 to 31.
    /// </summary>
    public static class ScheduleLayout
    {
        /// <summary>
        /// The narrowest a block is drawn, as a share of the bar, so short runs still show.
        /// </summary>
        private const double MinWidth = 0.006;

        /// <summary>
        /// The space kept between blocks in the same row, as a share of the bar.
        /// </summary>
        private const double Gap = 0.002;

        /// <summary>
        /// The most runs in a day before a task is drawn as a strip rather than blocks.
        /// </summary>
        private const int MostBlocksPerDay = 6;

        /// <summary>
        /// The most runs in an hour before a task on the hour bar is drawn as a strip rather than blocks.
        /// </summary>
        private const int MostBlocksPerHour = 30;

        /// <summary>
        /// Builds the four bars around now.
        /// </summary>
        /// <param name="tasks">The tasks, with their devices loaded.</param>
        /// <param name="nowUtc">The current time (UTC).</param>
        /// <param name="zone">The time zone the bars are drawn in.</param>
        /// <returns>The hour, day, week and month bars.</returns>
        public static List<ScheduleBar> Build(List<ScheduledTask> tasks, DateTime nowUtc, TimeZoneInfo zone)
        {
            DateTime now = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone);
            // The day runs noon to noon so overnight jobs sit in the middle, starting from yesterday's noon until today's comes round.
            DateTime noon = now.Date.AddHours(12);
            DateTime day = now < noon ? noon.AddDays(-1) : noon;
            DateTime week = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
            DateTime month = new(now.Year, now.Month, 1);
            DateTime hour = now.Date.AddHours(now.Hour);
            List<ScheduleTick> minutes = [.. Enumerable.Range(0, 13).Select(index => new ScheduleTick { Position = index / 12.0, Label = $":{index * 5 % 60:00}", Line = true })];
            List<ScheduleTick> hours = [.. Enumerable.Range(0, 25).Select(index => new ScheduleTick { Position = index / 24.0, Label = index % 3 == 0 ? day.AddHours(index).ToString("HH:mm") : string.Empty, Line = true })];
            List<ScheduleTick> weekdays = [.. Enumerable.Range(0, 8).Select(index => new ScheduleTick { Position = index / 7.0, Line = true }), .. Enumerable.Range(0, 7).Select(index => new ScheduleTick { Position = (index + 0.5) / 7, Label = week.AddDays(index).ToString("ddd") })];
            List<ScheduleTick> days = [.. Enumerable.Range(0, 32).Select(index => new ScheduleTick { Position = index / 31.0, Line = true }), .. Enumerable.Range(0, 31).Select(index => new ScheduleTick { Position = (index + 0.5) / 31, Label = (index + 1).ToString() })];
            return
            [
                Bar(SchedulePeriod.Hour, "Hour", $"Every hour, shown for {hour:HH:mm} to {hour.AddHours(1):HH:mm}", hour, TimeSpan.FromHours(1), minutes, tasks, nowUtc, zone),
                Bar(SchedulePeriod.Day, "Day", $"Noon {day:ddd d MMM} to noon {day.AddDays(1):ddd d MMM}", day, TimeSpan.FromDays(1), hours, tasks, nowUtc, zone),
                Bar(SchedulePeriod.Week, "Week", $"Monday {week:d MMM} to Sunday {week.AddDays(6):d MMM}", week, TimeSpan.FromDays(7), weekdays, tasks, nowUtc, zone),
                Bar(SchedulePeriod.Month, "Month", $"{month:MMMM yyyy}", month, TimeSpan.FromDays(31), days, tasks, nowUtc, zone)
            ];
        }

        /// <summary>
        /// Gets the name of the host a task runs against: its device, else its target as the source names it.
        /// </summary>
        /// <param name="task">The task, with its device loaded.</param>
        /// <returns>The host's name.</returns>
        public static string HostOf(ScheduledTask task) => task.Device?.Name ?? (task.Target.Length > 0 ? task.Target : "Unknown");

        /// <summary>
        /// Gets the time zone a task's schedule is in, falling back to UTC if it's unknown.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>The time zone.</returns>
        public static TimeZoneInfo ZoneOf(ScheduledTask task) => TimeZoneInfo.TryFindSystemTimeZoneById(task.TimeZone, out TimeZoneInfo? zone) ? zone : TimeZoneInfo.Utc;

        /// <summary>
        /// Gets how long a task usually takes, as the middle of its recent run times.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>The seconds, or null if it hasn't any runs to go on.</returns>
        public static int? Typical(ScheduledTask task)
        {
            List<int> ordered = [.. task.Durations.Order()];
            return ordered.Count == 0 ? null : ordered[ordered.Count / 2];
        }

        /// <summary>
        /// Builds one bar, placing every run of the tasks that belong on it and stacking runs that overlap.
        /// </summary>
        /// <param name="period">Which bar it is.</param>
        /// <param name="title">The heading.</param>
        /// <param name="span">The span in words.</param>
        /// <param name="start">Where it starts, in the bars' time zone.</param>
        /// <param name="length">How long it covers.</param>
        /// <param name="ticks">The marks along it.</param>
        /// <param name="tasks">Every task.</param>
        /// <param name="nowUtc">The current time (UTC).</param>
        /// <param name="zone">The time zone the bars are drawn in.</param>
        /// <returns>The bar.</returns>
        private static ScheduleBar Bar(SchedulePeriod period, string title, string span, DateTime start, TimeSpan length, List<ScheduleTick> ticks, List<ScheduledTask> tasks, DateTime nowUtc, TimeZoneInfo zone)
        {
            DateTime fromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(start, DateTimeKind.Unspecified), zone);
            DateTime toUtc = fromUtc + length;
            double Position(DateTime time) => (time - fromUtc).TotalSeconds / length.TotalSeconds;
            ScheduleBar bar = new() { Period = period, Title = title, Span = span, Ticks = ticks, Now = Position(nowUtc) };
            foreach (ScheduledTask task in tasks)
            {
                CronSchedule? cron = CronSchedule.Parse(task.Schedule);
                if (cron == null || cron.Period != period)
                {
                    continue;
                }
                List<DateTime> runs = [.. cron.Occurrences(fromUtc, toUtc, ZoneOf(task))];
                if (runs.Count == 0)
                {
                    continue;
                }
                string host = HostOf(task);
                bool crowded = period == SchedulePeriod.Hour ? runs.Count > MostBlocksPerHour : runs.GroupBy(run => TimeZoneInfo.ConvertTimeFromUtc(run, zone).Date).Max(group => group.Count()) > MostBlocksPerDay;
                if (crowded)
                {
                    bar.Strips.Add(new ScheduleStrip { TaskId = task.Id, Label = $"{task.Name} · {cron.Describe()}", Host = host, Positions = [.. runs.Select(Position)], Inactive = !task.Active });
                    continue;
                }
                double width = Math.Max(MinWidth, (Typical(task) ?? 0) / length.TotalSeconds);
                foreach (DateTime run in runs)
                {
                    double at = Position(run);
                    string time = TimeZoneInfo.ConvertTimeFromUtc(run, zone).ToString(period is SchedulePeriod.Hour or SchedulePeriod.Day ? "HH:mm" : "ddd d HH:mm");
                    bar.Blocks.Add(new ScheduleBlock { TaskId = task.Id, Name = task.Name, Title = $"{task.Name} · {host} · {time}", Host = host, Start = at, Width = Math.Min(width, 1 - at), Inactive = !task.Active });
                }
            }
            // Each block goes in the first row with room for it, so runs that overlap stack instead of hiding each other.
            List<double> ends = [];
            foreach (ScheduleBlock block in bar.Blocks.OrderBy(block => block.Start).ThenByDescending(block => block.Width))
            {
                int lane = ends.FindIndex(end => end + Gap <= block.Start);
                if (lane < 0)
                {
                    lane = ends.Count;
                    ends.Add(0);
                }
                ends[lane] = block.Start + block.Width;
                block.Lane = lane;
            }
            bar.Lanes = ends.Count;
            bar.Strips = [.. bar.Strips.OrderBy(strip => strip.Host).ThenBy(strip => strip.Label)];
            return bar;
        }
    }
}