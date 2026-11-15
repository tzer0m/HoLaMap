using HoLaMap.Models;

namespace HoLaMap.Schedules
{
    /// <summary>
    /// Lays the scheduled tasks out on the four bars: this hour, the day noon to noon, this week Monday to Sunday and this month 1 to 31.
    /// </summary>
    public static class ScheduleLayout
    {
        /// <summary>
        /// The shortest a block is drawn, as a share of the bar, so its name can be read. At the shortest the bars are drawn, 480px, this is about 14px.
        /// </summary>
        private const double MinWidth = 0.03;

        /// <summary>
        /// The space kept between blocks in the same row, as a share of the bar.
        /// </summary>
        private const double Gap = 0.004;

        /// <summary>
        /// The most runs in a day before a task is drawn as a strip rather than blocks.
        /// </summary>
        private const int MostBlocksPerDay = 6;

        /// <summary>
        /// The most runs in an hour before a task on the hour bar is drawn as a strip rather than blocks.
        /// </summary>
        private const int MostBlocksPerHour = 30;

        /// <summary>
        /// The least space between two labels along a bar, as a share of the bar.
        /// </summary>
        private const double MinLabelGap = 0.03;

        /// <summary>
        /// The most days a week or month bar can be trimmed to before it's marked in hours rather than days.
        /// </summary>
        private const int MostDaysForHours = 3;

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
            List<ScheduleTick> hours = [.. Enumerable.Range(0, 25).Select(index => new ScheduleTick { Position = index / 24.0, Label = day.AddHours(index).ToString("HH:mm"), Line = true })];
            List<ScheduleTick> weekdays = [.. Enumerable.Range(0, 8).Select(index => new ScheduleTick { Position = index / 7.0, Line = true }), .. Enumerable.Range(0, 7).Select(index => new ScheduleTick { Position = (index + 0.5) / 7, Label = week.AddDays(index).ToString("ddd") })];
            List<ScheduleTick> days = [.. Enumerable.Range(0, 32).Select(index => new ScheduleTick { Position = index / 31.0, Line = true }), .. Enumerable.Range(0, 31).Select(index => new ScheduleTick { Position = (index + 0.5) / 31, Label = (index + 1).ToString() })];
            Func<int, string> minute = index => $":{index * 5 % 60:00}";
            Func<int, string> clock = index => day.AddHours(index).ToString("HH:mm");
            return
            [
                Bar(new ScheduleBarSpec(SchedulePeriod.Hour, "Hour", $"Every hour, shown for {hour:HH:mm} to {hour.AddHours(1):HH:mm}", hour, TimeSpan.FromHours(1), 12, minute), minutes, tasks, zone),
                Bar(new ScheduleBarSpec(SchedulePeriod.Day, "Day", $"Noon {day:ddd d MMM} to noon {day.AddDays(1):ddd d MMM}", day, TimeSpan.FromDays(1), 24, clock), hours, tasks, zone),
                Bar(new ScheduleBarSpec(SchedulePeriod.Week, "Week", $"Monday {week:d MMM} to Sunday {week.AddDays(6):d MMM}", week, TimeSpan.FromDays(7), 7, null), weekdays, tasks, zone),
                Bar(new ScheduleBarSpec(SchedulePeriod.Month, "Month", $"{month:MMMM yyyy}", month, TimeSpan.FromDays(31), 31, null), days, tasks, zone)
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
        /// Builds one bar, placing every run of the tasks that belong on it, trimming the empty start and end and stacking runs that overlap.
        /// </summary>
        /// <param name="spec">What the bar covers.</param>
        /// <param name="ticks">The marks along it.</param>
        /// <param name="tasks">Every task.</param>
        /// <param name="zone">The time zone the bars are drawn in.</param>
        /// <returns>The bar.</returns>
        private static ScheduleBar Bar(ScheduleBarSpec spec, List<ScheduleTick> ticks, List<ScheduledTask> tasks, TimeZoneInfo zone)
        {
            DateTime fromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(spec.Start, DateTimeKind.Unspecified), zone);
            DateTime toUtc = fromUtc + spec.Length;
            double Position(DateTime time) => (time - fromUtc).TotalSeconds / spec.Length.TotalSeconds;
            ScheduleBar bar = new() { Period = spec.Period, Title = spec.Title, Span = spec.Span };
            foreach (ScheduledTask task in tasks)
            {
                CronSchedule? cron = CronSchedule.Parse(task.Schedule);
                if (cron == null || cron.Period != spec.Period)
                {
                    continue;
                }
                List<DateTime> runs = [.. cron.Occurrences(fromUtc, toUtc, ZoneOf(task))];
                if (runs.Count == 0)
                {
                    continue;
                }
                string host = HostOf(task);
                bool crowded = spec.Period == SchedulePeriod.Hour ? runs.Count > MostBlocksPerHour : runs.GroupBy(run => TimeZoneInfo.ConvertTimeFromUtc(run, zone).Date).Max(group => group.Count()) > MostBlocksPerDay;
                if (crowded)
                {
                    bar.Strips.Add(new ScheduleStrip { TaskId = task.Id, Label = $"{task.Name} · {cron.Describe()}", Host = host, Source = task.Source, Positions = [.. runs.Select(Position)], Inactive = !task.Active });
                    continue;
                }
                // Blocks start out as long as the run really takes, to work out where the bar can be trimmed, and get their least length once trimmed.
                double width = (Typical(task) ?? 0) / spec.Length.TotalSeconds;
                foreach (DateTime run in runs)
                {
                    double at = Position(run);
                    string time = TimeZoneInfo.ConvertTimeFromUtc(run, zone).ToString(spec.Period is SchedulePeriod.Hour or SchedulePeriod.Day ? "HH:mm" : "ddd d HH:mm");
                    bar.Blocks.Add(new ScheduleBlock { TaskId = task.Id, Name = task.Name, Title = $"{task.Name} · {host} · {time}", Host = host, Source = task.Source, Start = at, Width = Math.Min(width, 1 - at), Inactive = !task.Active });
                }
            }
            Trim(bar, new ScheduleTrim(Busy(spec.Cells, bar)), spec, ticks);
            // Each block goes in the first column with room for it, so runs that overlap sit side by side instead of hiding each other.
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

        /// <summary>
        /// Works out which cells of a bar have a run in them, from the blocks' real lengths and the strips' marks.
        /// </summary>
        /// <param name="cells">How many cells the bar is split into.</param>
        /// <param name="bar">The bar, with its runs placed but not yet trimmed.</param>
        /// <returns>Whether each cell is busy.</returns>
        private static bool[] Busy(int cells, ScheduleBar bar)
        {
            bool[] busy = new bool[cells];
            void Mark(double start, double end)
            {
                int first = Math.Clamp((int)Math.Floor(start * cells), 0, cells - 1);
                int last = Math.Clamp((int)Math.Ceiling(end * cells) - 1, first, cells - 1);
                for (int cell = first; cell <= last; cell++)
                {
                    busy[cell] = true;
                }
            }
            bar.Blocks.ForEach(block => Mark(block.Start, block.Start + block.Width));
            bar.Strips.ForEach(strip => strip.Positions.ForEach(position => Mark(position, position)));
            return busy;
        }

        /// <summary>
        /// Moves everything on a bar to its place on the trimmed bar, dropping the marks trimmed off and labelling the new ends. A week or month bar trimmed to a few days is marked in hours instead of days, so its times can be read.
        /// </summary>
        /// <param name="bar">The bar, with its runs placed.</param>
        /// <param name="trim">The trim to apply.</param>
        /// <param name="spec">What the bar covers.</param>
        /// <param name="ticks">The marks along it, before trimming.</param>
        private static void Trim(ScheduleBar bar, ScheduleTrim trim, ScheduleBarSpec spec, List<ScheduleTick> ticks)
        {
            foreach (ScheduleBlock block in bar.Blocks)
            {
                // A short run near the bottom is nudged up rather than cut short, so it still has room for its name.
                double start = trim.Map(block.Start);
                block.Width = Math.Min(Math.Max(MinWidth, trim.Map(block.Start + block.Width) - start), 1);
                block.Start = Math.Min(start, 1 - block.Width);
            }
            bar.Strips.ForEach(strip => strip.Positions = [.. strip.Positions.Select(trim.Map)]);
            int days = trim.Next - trim.First;
            if (spec.Period is SchedulePeriod.Week or SchedulePeriod.Month && days <= MostDaysForHours)
            {
                // One mark an hour for a single day, every two hours for two days and so on, with each day's start named instead of timed.
                ticks = [.. Enumerable.Range(0, 25).Select(index => trim.First * 24 + index * days).Select(hour => new ScheduleTick { Position = hour / (spec.Cells * 24.0), Label = hour % 24 == 0 ? spec.Start.AddHours(hour).ToString(spec.Period == SchedulePeriod.Week ? "ddd" : "d MMM") : spec.Start.AddHours(hour).ToString("HH:mm"), Line = true })];
            }
            List<ScheduleTick> kept = [.. ticks.Where(tick => !trim.Cuts(tick.Position))];
            if (spec.Edge != null)
            {
                // Trimming can cut off the labelled marks, so the new ends get labels of their own.
                kept.Add(new ScheduleTick { Position = trim.Start, Label = spec.Edge(trim.First) });
                kept.Add(new ScheduleTick { Position = trim.End, Label = spec.Edge(trim.Next) });
            }
            kept.ForEach(tick => tick.Position = trim.Map(tick.Position));
            // Labels that would sit on top of each other are thinned out, keeping the first.
            double last = double.MinValue;
            List<ScheduleTick> labels = [];
            foreach (ScheduleTick tick in kept.Where(tick => tick.Label.Length > 0).DistinctBy(tick => (tick.Label, Math.Round(tick.Position, 6))).OrderBy(tick => tick.Position))
            {
                if (tick.Position - last >= MinLabelGap)
                {
                    labels.Add(tick);
                    last = tick.Position;
                }
            }
            bar.Ticks = [.. kept.Where(tick => tick.Line).Select(tick => new ScheduleTick { Position = tick.Position, Line = true }), .. labels.Select(tick => new ScheduleTick { Position = tick.Position, Label = tick.Label })];
        }
    }
}