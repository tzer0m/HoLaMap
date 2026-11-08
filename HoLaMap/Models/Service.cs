namespace HoLaMap.Models
{
    /// <summary>
    /// A piece of software running on a device, such as a systemd unit, Docker container or TrueNAS app.
    /// </summary>
    public class Service
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the device it runs on.
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the device it runs on.
        /// </summary>
        public Device Device { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name, as the host reports it.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets how it runs.
        /// </summary>
        public ServiceKind Kind { get; set; }

        /// <summary>
        /// Gets or sets what it's for.
        /// </summary>
        public ServiceCategory Category { get; set; }

        /// <summary>
        /// Gets or sets whether it's one of your own apps rather than third-party software.
        /// </summary>
        public bool Mine { get; set; }

        /// <summary>
        /// Gets or sets where it came from.
        /// </summary>
        public ServiceSource Source { get; set; }

        /// <summary>
        /// Gets or sets when its host last reported it (UTC), or null for one added by hand.
        /// </summary>
        public DateTime? LastSeen { get; set; }
    }
}