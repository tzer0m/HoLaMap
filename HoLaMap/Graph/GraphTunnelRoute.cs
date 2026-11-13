namespace HoLaMap.Graph
{
    /// <summary>
    /// A tunnel's public hostname and the service it reaches, as the map draws it.
    /// </summary>
    public class GraphTunnelRoute
    {
        /// <summary>
        /// The route's ID on the map.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The public hostname.
        /// </summary>
        public string Hostname { get; set; } = string.Empty;

        /// <summary>
        /// The path it's limited to, or an empty string for the whole hostname.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Where the tunnel sends it.
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// The map ID of the service it reaches, or an empty string if none was found.
        /// </summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>
        /// Whether the service was picked by hand rather than matched by port.
        /// </summary>
        public bool Manual { get; set; }
    }
}