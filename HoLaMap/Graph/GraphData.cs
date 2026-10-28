namespace HoLaMap.Graph
{
    /// <summary>
    /// Everything needed to draw the map.
    /// </summary>
    public class GraphData
    {
        /// <summary>
        /// The devices.
        /// </summary>
        public List<GraphNode> Nodes { get; set; } = [];

        /// <summary>
        /// The links.
        /// </summary>
        public List<GraphEdge> Edges { get; set; } = [];

        /// <summary>
        /// The floors and their rooms.
        /// </summary>
        public List<GraphFloor> Floors { get; set; } = [];

        /// <summary>
        /// The racks.
        /// </summary>
        public List<GraphRack> Racks { get; set; } = [];
    }
}