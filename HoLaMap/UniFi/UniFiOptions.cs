namespace HoLaMap.UniFi
{
    /// <summary>
    /// Settings for the UniFi Network API.
    /// </summary>
    public class UniFiOptions
    {
        /// <summary>
        /// Gets or sets the console base URL, e.g. https://192.168.1.1/.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the site name.
        /// </summary>
        public string Site { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of minutes between syncs.
        /// </summary>
        public int SyncIntervalMinutes { get; set; }
    }
}