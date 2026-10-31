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
    }
}