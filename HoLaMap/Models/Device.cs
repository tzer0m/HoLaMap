namespace HoLaMap.Models
{
    /// <summary>
    /// A physical or virtual device on the network, identified by its MAC address.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the kind of device.
        /// </summary>
        public DeviceKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the model, if known.
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the MAC address in lowercase colon format.
        /// </summary>
        public string Mac { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last known IP address.
        /// </summary>
        public string? Ip { get; set; }

        /// <summary>
        /// Gets or sets when the device was last seen (UTC).
        /// </summary>
        public DateTime LastSeen { get; set; }

        /// <summary>
        /// Gets or sets the device's ports.
        /// </summary>
        public List<Port> Ports { get; set; } = [];
    }
}