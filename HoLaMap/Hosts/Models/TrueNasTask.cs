namespace HoLaMap.Hosts.Models
{
    /// <summary>
    /// A TrueNAS task as its host reported it, without its command or credentials.
    /// </summary>
    public class TrueNasTask
    {
        /// <summary>
        /// Gets or sets what kind of task it is, such as cronjob, cloudsync, rsync, scrub, snapshot or replication.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets TrueNAS's ID for it, unique within its kind.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets what it does in a few words, like Cloud Sync, pull /mnt/pool1/backups.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when it runs as a five-field cron expression.
        /// </summary>
        public string Schedule { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether it's turned on.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets its recorded runs, in any order.
        /// </summary>
        public List<TrueNasRun> Runs { get; set; } = [];
    }
}