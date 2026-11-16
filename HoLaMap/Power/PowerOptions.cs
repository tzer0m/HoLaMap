namespace HoLaMap.Power
{
    /// <summary>
    /// Settings for checking, waking and powering off machines.
    /// </summary>
    public class PowerOptions
    {
        /// <summary>
        /// Gets or sets how long to wait for a ping or port check, in milliseconds.
        /// </summary>
        public int CheckTimeoutMs { get; set; } = 1500;

        /// <summary>
        /// Gets or sets how often the Power page refreshes each machine's status, in milliseconds.
        /// </summary>
        public int RefreshIntervalMs { get; set; } = 10000;

        /// <summary>
        /// Gets or sets how long to wait for the wake command, in milliseconds.
        /// </summary>
        public int CommandTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the path to sudo.
        /// </summary>
        public string SudoPath { get; set; } = "/usr/bin/sudo";

        /// <summary>
        /// Gets or sets the path to etherwake.
        /// </summary>
        public string EtherWakePath { get; set; } = "/usr/sbin/etherwake";
    }
}