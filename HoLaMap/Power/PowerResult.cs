namespace HoLaMap.Power
{
    /// <summary>
    /// The outcome of a wake, sleep or power off.
    /// </summary>
    public class PowerResult
    {
        /// <summary>
        /// Gets or sets whether it worked.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the message shown on the Power page.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}