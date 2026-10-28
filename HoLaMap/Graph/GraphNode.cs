namespace HoLaMap.Graph
{
    /// <summary>
    /// A device drawn on the map.
    /// </summary>
    public class GraphNode
    {
        /// <summary>
        /// The node ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The text shown on the node.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// The device kind in lowercase, used for styling.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// Extra details shown when the node is selected.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// The device's ports in order.
        /// </summary>
        public List<GraphPort> Ports { get; set; } = [];

        /// <summary>
        /// Whether the device was seen by the last few syncs.
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// The ID of the room the device is in, or empty if unplaced.
        /// </summary>
        public string Room { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the rack the device is in, or empty if it isn't in one.
        /// </summary>
        public string Rack { get; set; } = string.Empty;
    }
}