namespace HoLaMap.Models
{
    /// <summary>
    /// A connection from a port to another device (and optionally a specific port on it).
    /// </summary>
    public class Link
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the source port identifier.
        /// </summary>
        public int FromPortId { get; set; }

        /// <summary>
        /// Gets or sets the source port.
        /// </summary>
        public Port FromPort { get; set; } = null!;

        /// <summary>
        /// Gets or sets the destination device identifier.
        /// </summary>
        public int ToDeviceId { get; set; }

        /// <summary>
        /// Gets or sets the destination device.
        /// </summary>
        public Device ToDevice { get; set; } = null!;

        /// <summary>
        /// Gets or sets the destination port identifier, if known.
        /// </summary>
        public int? ToPortId { get; set; }

        /// <summary>
        /// Gets or sets the destination port, if known.
        /// </summary>
        public Port? ToPort { get; set; }

        /// <summary>
        /// Gets or sets where the link came from.
        /// </summary>
        public LinkSource Source { get; set; }

        /// <summary>
        /// Gets or sets optional notes about the link.
        /// </summary>
        public string? Notes { get; set; }
    }
}