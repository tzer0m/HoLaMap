namespace HoLaMap.Models
{
    /// <summary>
    /// A device the sync saw on a port that is already assigned to something else, waiting for a decision.
    /// </summary>
    public class ActionItem
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the port the device was seen on.
        /// </summary>
        public int PortId { get; set; }

        /// <summary>
        /// Gets or sets the port the device was seen on.
        /// </summary>
        public Port Port { get; set; } = null!;

        /// <summary>
        /// Gets or sets the device that was seen.
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the device that was seen.
        /// </summary>
        public Device Device { get; set; } = null!;

        /// <summary>
        /// Gets or sets when the collision was first seen (UTC).
        /// </summary>
        public DateTime FirstSeen { get; set; }

        /// <summary>
        /// Gets or sets when the collision was last seen (UTC).
        /// </summary>
        public DateTime LastSeen { get; set; }

        /// <summary>
        /// Gets or sets whether the item has been dismissed.
        /// </summary>
        public bool Dismissed { get; set; }
    }
}