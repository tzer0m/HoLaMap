namespace HoLaMap.Graph
{
    /// <summary>
    /// A Cloudflare tunnel with its routes, as the map draws it.
    /// </summary>
    public class GraphTunnel
    {
        /// <summary>
        /// The tunnel's ID on the map.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The tunnel's name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Cloudflare's status for it, such as healthy or down.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// The cloudflared version its connector runs, or an empty string if unknown.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Whether its routes are set in the Cloudflare dashboard rather than cloudflared's config file.
        /// </summary>
        public bool RemoteConfig { get; set; }

        /// <summary>
        /// The map ID of the device it runs on, or an empty string if unknown.
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Its public hostnames.
        /// </summary>
        public List<GraphTunnelRoute> Routes { get; set; } = [];
    }
}