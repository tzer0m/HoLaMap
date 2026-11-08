using HoLaMap.Models;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Settings for hosts reporting the services running on them.
    /// </summary>
    public class HostReportOptions
    {
        /// <summary>
        /// Gets or sets the token hosts send with their reports.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets how many days a service that has gone from its host is kept before it's forgotten, or 0 to keep it forever.
        /// </summary>
        public int ForgetAfterDays { get; set; } = 30;

        /// <summary>
        /// Gets or sets systemd services to keep even though they came with a package, without the .service suffix.
        /// </summary>
        public List<string> SystemdInclude { get; set; } = [];

        /// <summary>
        /// Gets or sets the category of each service by name, where a trailing * matches the start of the name. Anything else is an app.
        /// </summary>
        public Dictionary<string, ServiceCategory> Categories { get; set; } = [];
    }
}