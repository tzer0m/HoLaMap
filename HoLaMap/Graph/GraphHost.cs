namespace HoLaMap.Graph
{
    /// <summary>
    /// A host drawn on the Services view, holding its services.
    /// </summary>
    public class GraphHost
    {
        /// <summary>
        /// The host's device ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The host name.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Whether the host has been seen recently.
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// The services on the host.
        /// </summary>
        public List<GraphService> Services { get; set; } = [];
    }
}