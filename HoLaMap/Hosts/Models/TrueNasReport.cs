namespace HoLaMap.Hosts.Models
{
    /// <summary>
    /// A TrueNAS host's scheduled tasks, as the report script sends them.
    /// </summary>
    public class TrueNasReport
    {
        /// <summary>
        /// Gets or sets the host's time zone, which its schedules are in.
        /// </summary>
        public string? TimeZone { get; set; }

        /// <summary>
        /// Gets or sets the tasks.
        /// </summary>
        public List<TrueNasTask> Tasks { get; set; } = [];
    }
}