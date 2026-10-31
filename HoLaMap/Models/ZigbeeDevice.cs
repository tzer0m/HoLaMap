namespace HoLaMap.Models
{
    /// <summary>
    /// A Zigbee device read from Home Assistant's ZHA integration.
    /// </summary>
    public class ZigbeeDevice
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the IEEE address, which identifies the device across syncs.
        /// </summary>
        public string Ieee { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the device name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the manufacturer.
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the role: Coordinator, Router or EndDevice.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the power source, e.g. Mains or Battery.
        /// </summary>
        public string? PowerSource { get; set; }

        /// <summary>
        /// Gets or sets the last link quality seen, from 0 to 255.
        /// </summary>
        public int? Lqi { get; set; }

        /// <summary>
        /// Gets or sets the last signal strength in dBm.
        /// </summary>
        public int? Rssi { get; set; }

        /// <summary>
        /// Gets or sets when the device was last heard from (UTC).
        /// </summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// Gets or sets whether ZHA considers the device available.
        /// </summary>
        public bool Available { get; set; }

        /// <summary>
        /// Gets or sets the room identifier. Set by hand and never changed by the sync.
        /// </summary>
        public int? RoomId { get; set; }

        /// <summary>
        /// Gets or sets the room the device is in.
        /// </summary>
        public Room? Room { get; set; }

        /// <summary>
        /// Gets or sets the device's neighbour table.
        /// </summary>
        public List<ZigbeeNeighbor> Neighbors { get; set; } = [];
    }
}