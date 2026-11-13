namespace HoLaMap.Cloudflare
{
    /// <summary>
    /// Settings for reading tunnels from Cloudflare.
    /// </summary>
    public class CloudflareOptions
    {
        /// <summary>
        /// Gets or sets the Cloudflare account ID.
        /// </summary>
        public string AccountId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an API token with read access to tunnels.
        /// </summary>
        public string ApiToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of minutes between syncs.
        /// </summary>
        public int SyncIntervalMinutes { get; set; } = 60;
    }
}