namespace HoLaMap.Hosts.Models
{
    /// <summary>
    /// A service as a host reported it.
    /// </summary>
    public class ReportedService
    {
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether it was set up by hand rather than coming with a package.
        /// </summary>
        public bool Custom { get; set; }

        /// <summary>
        /// Gets or sets the group it was started with, such as its Docker Compose project, if any.
        /// </summary>
        public string? Group { get; set; }
    }
}