namespace HoLaMap.Graph
{
    /// <summary>
    /// Everything the Services and Cloudflare views draw: the hosts with their services, the links between services, and the tunnels reaching them.
    /// </summary>
    public class GraphServiceMap
    {
        /// <summary>
        /// The hosts, and the cloud, with their services.
        /// </summary>
        public List<GraphHost> Hosts { get; set; } = [];

        /// <summary>
        /// The links between services.
        /// </summary>
        public List<GraphServiceLink> Links { get; set; } = [];

        /// <summary>
        /// The Cloudflare tunnels, with each route's service.
        /// </summary>
        public List<GraphTunnel> Tunnels { get; set; } = [];
    }
}