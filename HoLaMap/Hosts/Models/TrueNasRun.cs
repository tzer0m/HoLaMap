namespace HoLaMap.Hosts.Models
{
    /// <summary>
    /// One run of a TrueNAS task, from the middleware's job records.
    /// </summary>
    public class TrueNasRun
    {
        /// <summary>
        /// Gets or sets when it started, in milliseconds since 1970 (UTC), if known.
        /// </summary>
        public long? Start { get; set; }

        /// <summary>
        /// Gets or sets when it finished, in milliseconds since 1970 (UTC), if it has.
        /// </summary>
        public long? End { get; set; }

        /// <summary>
        /// Gets or sets TrueNAS's state for it, such as SUCCESS, FAILED or RUNNING.
        /// </summary>
        public string? State { get; set; }
    }
}