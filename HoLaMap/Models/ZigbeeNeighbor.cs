namespace HoLaMap.Models
{
    /// <summary>
    /// An entry in a Zigbee device's neighbour table.
    /// </summary>
    public class ZigbeeNeighbor
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the device whose table this is.
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the device whose table this is.
        /// </summary>
        public ZigbeeDevice Device { get; set; } = null!;

        /// <summary>
        /// Gets or sets the neighbour's IEEE address.
        /// </summary>
        public string NeighborIeee { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the link quality to the neighbour, from 0 to 255.
        /// </summary>
        public int? Lqi { get; set; }

        /// <summary>
        /// Gets or sets how the neighbour is related: Parent, Child, Sibling, None or Previous_Child.
        /// </summary>
        public string? Relationship { get; set; }
    }
}