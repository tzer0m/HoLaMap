namespace HoLaMap.Graph
{
    /// <summary>
    /// A port square drawn on a device.
    /// </summary>
    public class GraphPort
    {
        /// <summary>
        /// The port ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The port number shown in the square.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The port CSS class, used for the square's colour.
        /// </summary>
        public string Style { get; set; } = string.Empty;

        /// <summary>
        /// The tooltip and details for the port.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// The network (VLAN) the port carries untagged.
        /// </summary>
        public string Network { get; set; } = string.Empty;

        /// <summary>
        /// The networks (VLANs) the port carries tagged.
        /// </summary>
        public List<string> Tagged { get; set; } = [];

        /// <summary>
        /// The port name, or empty if it has none.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether the port is up.
        /// </summary>
        public bool Up { get; set; }

        /// <summary>
        /// The link speed in Mbps, or null when the port is down.
        /// </summary>
        public int? Speed { get; set; }

        /// <summary>
        /// Whether the port can supply PoE.
        /// </summary>
        public bool PoECapable { get; set; }

        /// <summary>
        /// The PoE power being drawn in watts, or null if none.
        /// </summary>
        public double? PoEPower { get; set; }
    }
}