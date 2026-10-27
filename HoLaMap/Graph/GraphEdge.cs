namespace HoLaMap.Graph
{
    /// <summary>
    /// A link drawn on the map.
    /// </summary>
    public class GraphEdge
    {
        /// <summary>
        /// The edge ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The upstream node ID.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// The downstream node ID.
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// The upstream port ID the line leaves from.
        /// </summary>
        public string SourcePort { get; set; } = string.Empty;

        /// <summary>
        /// The downstream port ID, if known.
        /// </summary>
        public string TargetPort { get; set; } = string.Empty;

        /// <summary>
        /// The port numbers shown on the edge.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// The port CSS class, used for the edge colour.
        /// </summary>
        public string Style { get; set; } = string.Empty;

        /// <summary>
        /// Whether the link was added by hand.
        /// </summary>
        public bool Manual { get; set; }

        /// <summary>
        /// Whether the upstream port is supplying PoE power over this link.
        /// </summary>
        public bool PoE { get; set; }

        /// <summary>
        /// Extra details shown when the edge is selected.
        /// </summary>
        public string Detail { get; set; } = string.Empty;
    }
}