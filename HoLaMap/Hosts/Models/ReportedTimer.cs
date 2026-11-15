namespace HoLaMap.Hosts.Models
{
    /// <summary>
    /// A systemd timer as its host reported it, with the last run of the unit it starts.
    /// </summary>
    public class ReportedTimer
    {
        /// <summary>
        /// Gets or sets the name, without the .timer suffix.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unit it starts, like TNRelay.service.
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when it runs as a cron expression, or as systemd wrote it if it can't be put that way.
        /// </summary>
        public string Schedule { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the timer is running, so will fire.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets when the unit last started (UTC), if it has.
        /// </summary>
        public DateTime? LastStart { get; set; }

        /// <summary>
        /// Gets or sets when the unit last finished (UTC), if it has since it last started.
        /// </summary>
        public DateTime? LastEnd { get; set; }

        /// <summary>
        /// Gets or sets how the unit's last run ended, such as success or exit-code.
        /// </summary>
        public string? Result { get; set; }
    }
}