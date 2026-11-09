using HoLaMap.Hosts.Models;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Settings for reading API routes from OpenAPI descriptions.
    /// </summary>
    public class OpenApiOptions
    {
        /// <summary>
        /// Gets or sets the number of minutes between syncs.
        /// </summary>
        public int SyncIntervalMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets the APIs to read.
        /// </summary>
        public List<OpenApiSource> Sources { get; set; } = [];
    }
}