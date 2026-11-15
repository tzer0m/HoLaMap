namespace HoLaMap.Models
{
    /// <summary>
    /// Something that runs on a schedule somewhere in the fleet, such as a Semaphore template run nightly.
    /// </summary>
    public class ScheduledTask
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets where it was read from.
        /// </summary>
        public ScheduleSource Source { get; set; }

        /// <summary>
        /// Gets or sets its ID in the source, such as the Semaphore schedule ID.
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the schedule as a five-field cron expression.
        /// </summary>
        public string Schedule { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time zone the schedule's times are in, such as UTC or Europe/London.
        /// </summary>
        public string TimeZone { get; set; } = "UTC";

        /// <summary>
        /// Gets or sets what it runs, such as a playbook path.
        /// </summary>
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets what it runs against as the source names it, such as a Semaphore inventory.
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the device it runs against, if one matches the target.
        /// </summary>
        public int? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the device it runs against, if one matches the target.
        /// </summary>
        public Device? Device { get; set; }

        /// <summary>
        /// Gets or sets whether the schedule is turned on.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets when it last started (UTC), if known.
        /// </summary>
        public DateTime? LastRun { get; set; }

        /// <summary>
        /// Gets or sets how its last run ended, such as success or error.
        /// </summary>
        public string? LastStatus { get; set; }

        /// <summary>
        /// Gets or sets how long its recent finished runs took in seconds, newest first.
        /// </summary>
        public List<int> Durations { get; set; } = [];

        /// <summary>
        /// Gets or sets when it was last read from its source (UTC).
        /// </summary>
        public DateTime LastSeen { get; set; }
    }
}