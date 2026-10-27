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
    }
}