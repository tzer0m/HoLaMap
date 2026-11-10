namespace HoLaMap.Graph
{
    /// <summary>
    /// Everything the Services view draws: the hosts with their services, and the links between services.
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
    }
}