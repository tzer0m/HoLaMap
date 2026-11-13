namespace HoLaMap.Models
{
    /// <summary>
    /// A public hostname on a tunnel and where the tunnel sends it.
    /// </summary>
    public class TunnelRoute
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the tunnel.
        /// </summary>
        public int TunnelId { get; set; }

        /// <summary>
        /// Gets or sets the tunnel.
        /// </summary>
        public Tunnel Tunnel { get; set; } = null!;

        /// <summary>
        /// Gets or sets the public hostname, such as pc.tzer0m.co.uk.
        /// </summary>
        public string Hostname { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path it's limited to, or an empty string for the whole hostname.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets where the tunnel sends it, such as http://localhost:5152.
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the service it reaches, when picked by hand, or null to match it by port.
        /// </summary>
        public int? ServiceId { get; set; }

        /// <summary>
        /// Gets or sets the service it reaches, when picked by hand.
        /// </summary>
        public Service? Service { get; set; }
    }
}