namespace HoLaMap.Hosts.Models
{
    /// <summary>
    /// A unit as reported by systemctl show.
    /// </summary>
    public class SystemdUnit
    {
        /// <summary>
        /// Gets or sets the unit name, e.g. stockwise.service.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the path of the unit file.
        /// </summary>
        public string? FragmentPath { get; set; }

        /// <summary>
        /// Gets or sets the active state, e.g. active or inactive.
        /// </summary>
        public string? ActiveState { get; set; }

        /// <summary>
        /// Gets or sets the sub-state, e.g. running or dead.
        /// </summary>
        public string? SubState { get; set; }
    }
}