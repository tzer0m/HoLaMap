using HoLaMap.Data;
using HoLaMap.Hosts.Models;
using HoLaMap.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Saves the services a host reports. Services added by hand are left alone.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="options">The host report settings, read fresh for each report so changes to the categories apply without a restart.</param>
    public class HostSyncService(HoLaMapContext context, IOptionsSnapshot<HostReportOptions> options)
    {
        /// <summary>
        /// The status kept for a timer's run that hasn't finished yet.
        /// </summary>
        private const string Running = "running";

        /// <summary>
        /// Updates one kind of synced service on a host to match what it just reported.
        /// </summary>
        /// <param name="host">The host's device name or MAC address.</param>
        /// <param name="kind">The kind of service reported.</param>
        /// <param name="reported">The services running.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scope">The one group the report covers, such as one API's routes, or null if it covers every service of its kind on the host.</param>
        /// <returns>The number of services reported, or null if no device matches the host.</returns>
        public async Task<int?> SyncAsync(string host, ServiceKind kind, List<ReportedService> reported, CancellationToken cancellationToken, string? scope = null)
        {
            string name = host.Trim().ToLowerInvariant();
            string mac = name.Replace('-', ':');
            Device? device = await context.Devices.Include(item => item.Services).FirstOrDefaultAsync(item => item.Name.ToLower() == name || item.Mac == mac, cancellationToken);
            if (device == null)
            {
                return null;
            }
            DateTime now = DateTime.UtcNow;
            // A service is known by its group and name, since containers in different Compose projects can share a name like postgres.
            HashSet<(string, string)> keys = [.. reported.Select(item => (item.Group ?? string.Empty, item.Name))];

            // A service that has gone from its host stays greyed out in case it comes back, and is forgotten once it hasn't been reported for a while.
            int forgetAfterDays = options.Value.ForgetAfterDays;
            foreach (Service missing in device.Services.Where(service => service.Kind == kind && service.Source == ServiceSource.Synced && (scope == null || service.Group == scope) && !keys.Contains((service.Group ?? string.Empty, service.Name))).ToList())
            {
                if (forgetAfterDays > 0 && missing.LastSeen < now.AddDays(-forgetAfterDays))
                {
                    context.Services.Remove(missing);
                }
            }
            foreach (ReportedService item in reported)
            {
                Service? service = device.Services.FirstOrDefault(existing => existing.Kind == kind && existing.Name == item.Name && (existing.Group ?? string.Empty) == (item.Group ?? string.Empty));
                if (service == null)
                {
                    service = new Service { Name = item.Name, Kind = kind, Source = ServiceSource.Synced, Group = item.Group };
                    device.Services.Add(service);
                }
                if (service.Source == ServiceSource.Synced)
                {
                    service.LastSeen = now;
                    service.Category = CategoryOf(item.Name, item.Custom);
                    // It's one of your own apps if it was set up by hand and isn't a tool, like a runner, or monitoring, like a hand-installed node_exporter.
                    service.Mine = item.Custom && service.Category is ServiceCategory.Custom or ServiceCategory.Home or ServiceCategory.Media or ServiceCategory.Data;
                    // Only Docker reports say which ports a service uses. Systemd services get theirs from the ports report instead.
                    if (kind == ServiceKind.Docker)
                    {
                        service.Ports = item.Ports;
                    }
                }
            }
            await context.SaveChangesAsync(cancellationToken);
            return keys.Count;
        }

        /// <summary>
        /// Saves which ports each of a host's synced systemd services listens on, matching each listening process to the unit with its name.
        /// </summary>
        /// <param name="host">The host's device name or MAC address.</param>
        /// <param name="listening">The ports and the processes holding them.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of services with a port, or null if no device matches the host.</returns>
        public async Task<int?> SyncPortsAsync(string host, List<ListeningPort> listening, CancellationToken cancellationToken)
        {
            string name = host.Trim().ToLowerInvariant();
            string mac = name.Replace('-', ':');
            Device? device = await context.Devices.Include(item => item.Services).FirstOrDefaultAsync(item => item.Name.ToLower() == name || item.Mac == mac, cancellationToken);
            if (device == null)
            {
                return null;
            }
            int count = 0;
            foreach (Service service in device.Services.Where(service => service.Kind == ServiceKind.Systemd && service.Source == ServiceSource.Synced))
            {
                // Linux cuts process names to 15 characters, so a longer unit name only has to start with it. An app named after its project, like Connect4.Web for Connect4, counts too.
                service.Ports = [.. listening.Where(item => item.Process.Equals(service.Name, StringComparison.OrdinalIgnoreCase) || (item.Process.Length == 15 && service.Name.StartsWith(item.Process, StringComparison.OrdinalIgnoreCase)) || item.Process.StartsWith($"{service.Name}.", StringComparison.OrdinalIgnoreCase)).Select(item => item.Port).Distinct().Order()];
                count += service.Ports.Count > 0 ? 1 : 0;
            }
            await context.SaveChangesAsync(cancellationToken);
            return count;
        }

        /// <summary>
        /// Updates a host's systemd timers to match what it just reported, adding the latest run's duration when it has finished. Timers no longer reported are removed.
        /// </summary>
        /// <param name="host">The host's device name or MAC address.</param>
        /// <param name="timeZone">The host's time zone, which its calendar times are in, or null if it didn't say.</param>
        /// <param name="timers">The timers.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of timers, or null if no device matches the host.</returns>
        public async Task<int?> SyncTimersAsync(string host, string? timeZone, List<ReportedTimer> timers, CancellationToken cancellationToken)
        {
            string name = host.Trim().ToLowerInvariant();
            string mac = name.Replace('-', ':');
            Device? device = await context.Devices.FirstOrDefaultAsync(item => item.Name.ToLower() == name || item.Mac == mac, cancellationToken);
            if (device == null)
            {
                return null;
            }
            List<ScheduledTask> tasks = await context.ScheduledTasks.Where(task => task.Source == ScheduleSource.SystemdTimer && task.DeviceId == device.Id).ToListAsync(cancellationToken);
            context.ScheduledTasks.RemoveRange(tasks.Where(task => !timers.Any(timer => task.ExternalId == $"{device.Id}:{timer.Name}")));
            DateTime now = DateTime.UtcNow;
            foreach (ReportedTimer timer in timers)
            {
                string id = $"{device.Id}:{timer.Name}";
                ScheduledTask? task = tasks.FirstOrDefault(existing => existing.ExternalId == id);
                if (task == null)
                {
                    task = new ScheduledTask { Source = ScheduleSource.SystemdTimer, ExternalId = id, DeviceId = device.Id };
                    context.ScheduledTasks.Add(task);
                }
                // A run is counted once it has finished: either it's a new run, or the one last seen still going has since ended.
                bool finished = timer.LastStart != null && timer.LastEnd != null && (task.LastRun != timer.LastStart || task.LastStatus == Running);
                if (finished && timer.Result == "success")
                {
                    task.Durations = [(int)Math.Round((timer.LastEnd!.Value - timer.LastStart!.Value).TotalSeconds), .. task.Durations.Take(options.Value.TimerDurations - 1)];
                }
                task.Name = timer.Name;
                task.Schedule = timer.Schedule;
                task.TimeZone = string.IsNullOrEmpty(timeZone) ? "UTC" : timeZone;
                task.Command = timer.Unit;
                task.Target = device.Name;
                task.Active = timer.Active;
                task.LastRun = timer.LastStart;
                task.LastStatus = timer.LastStart == null ? null : timer.LastEnd == null ? Running : timer.Result;
                task.LastSeen = now;
            }
            await context.SaveChangesAsync(cancellationToken);
            return timers.Count;
        }

        /// <summary>
        /// Updates a TrueNAS host's tasks to match what it just reported, adding the durations of runs finished since the last report. Tasks no longer reported are removed.
        /// </summary>
        /// <param name="host">The host's device name or MAC address.</param>
        /// <param name="report">The report.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of tasks, or null if no device matches the host.</returns>
        public async Task<int?> SyncTrueNasAsync(string host, TrueNasReport report, CancellationToken cancellationToken)
        {
            string name = host.Trim().ToLowerInvariant();
            string mac = name.Replace('-', ':');
            Device? device = await context.Devices.FirstOrDefaultAsync(item => item.Name.ToLower() == name || item.Mac == mac, cancellationToken);
            if (device == null)
            {
                return null;
            }
            List<ScheduledTask> tasks = await context.ScheduledTasks.Where(task => task.Source == ScheduleSource.TrueNas && task.DeviceId == device.Id).ToListAsync(cancellationToken);
            HashSet<string> ids = [.. report.Tasks.Select(item => $"{device.Id}:{item.Kind}:{item.Id}")];
            context.ScheduledTasks.RemoveRange(tasks.Where(task => !ids.Contains(task.ExternalId)));
            DateTime now = DateTime.UtcNow;
            foreach (TrueNasTask item in report.Tasks)
            {
                string id = $"{device.Id}:{item.Kind}:{item.Id}";
                ScheduledTask? task = tasks.FirstOrDefault(existing => existing.ExternalId == id);
                if (task == null)
                {
                    task = new ScheduledTask { Source = ScheduleSource.TrueNas, ExternalId = id, DeviceId = device.Id };
                    context.ScheduledTasks.Add(task);
                }
                List<TrueNasRun> runs = [.. item.Runs.Where(run => run.Start != null).OrderByDescending(run => run.Start)];
                // Runs that finished since the last report add their durations, newest first. The run last seen still going counts too once it ends.
                DateTime? since = task.LastRun;
                bool waiting = task.LastStatus == Running;
                List<int> added = [.. runs.Where(run => run.End != null && Status(run.State) == "success" && (since == null || Time(run.Start) > since || (waiting && Time(run.Start) == since))).Select(run => (int)Math.Round((run.End!.Value - run.Start!.Value) / 1000.0))];
                task.Durations = [.. added.Concat(task.Durations).Take(options.Value.TimerDurations)];
                task.Name = item.Name;
                task.Schedule = item.Schedule;
                task.TimeZone = string.IsNullOrEmpty(report.TimeZone) ? "UTC" : report.TimeZone;
                task.Command = item.Detail;
                task.Target = device.Name;
                task.Active = item.Enabled;
                TrueNasRun? last = runs.FirstOrDefault();
                task.LastRun = last == null ? task.LastRun : Time(last.Start);
                task.LastStatus = last == null ? task.LastStatus : last.End == null && Status(last.State) == "success" ? "success" : Status(last.State);
                task.LastSeen = now;
            }
            await context.SaveChangesAsync(cancellationToken);
            return report.Tasks.Count;
        }

        /// <summary>
        /// Turns a TrueNAS job state into the statuses used elsewhere, like success or failed.
        /// </summary>
        /// <param name="state">The state, such as SUCCESS, FINISHED, FAILED or RUNNING.</param>
        /// <returns>The status.</returns>
        private static string? Status(string? state) => state == null ? null : state.ToUpperInvariant() switch
        {
            "SUCCESS" or "FINISHED" => "success",
            "FAILED" or "ERROR" => "failed",
            "RUNNING" or "WAITING" or "PENDING" => Running,
            _ => state.ToLowerInvariant()
        };

        /// <summary>
        /// Reads a time in milliseconds since 1970.
        /// </summary>
        /// <param name="milliseconds">The milliseconds, or null.</param>
        /// <returns>The time (UTC), or null.</returns>
        private static DateTime? Time(long? milliseconds) => milliseconds == null ? null : DateTime.UnixEpoch.AddMilliseconds(milliseconds.Value);

        /// <summary>
        /// Works out a service's category from the settings, where a trailing * matches the start of the name.
        /// </summary>
        /// <param name="name">The service name.</param>
        /// <param name="custom">Whether the service was set up by hand, like your own apps' systemd units and API routes.</param>
        /// <returns>The category, or Custom for anything set up by hand and Other for anything else if none matches.</returns>
        private ServiceCategory CategoryOf(string name, bool custom)
        {
            foreach (KeyValuePair<string, ServiceCategory> pair in options.Value.Categories)
            {
                bool matches = pair.Key.EndsWith('*') ? name.StartsWith(pair.Key[..^1], StringComparison.OrdinalIgnoreCase) : name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase);
                if (matches)
                {
                    return pair.Value;
                }
            }
            return custom ? ServiceCategory.Custom : ServiceCategory.Other;
        }
    }
}