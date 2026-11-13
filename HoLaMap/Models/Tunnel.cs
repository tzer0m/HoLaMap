namespace HoLaMap.Models
{
    /// <summary>
    /// A Cloudflare Tunnel, read from Cloudflare.
    /// </summary>
    public class Tunnel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets Cloudflare's ID for the tunnel.
        /// </summary>
        public string CloudflareId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets Cloudflare's status for it, such as healthy or down.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the cloudflared version its connector runs, if it's connected.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets whether its routes are set in the Cloudflare dashboard rather than in cloudflared's config file.
        /// </summary>
        public bool RemoteConfig { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the device its connector runs on, when picked by hand, or null to use the device with the tunnel's name.
        /// </summary>
        public int? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the device its connector runs on, when picked by hand.
        /// </summary>
        public Device? Device { get; set; }

        /// <summary>
        /// Gets or sets its routes.
        /// </summary>
        public List<TunnelRoute> Routes { get; set; } = [];

        /// <summary>
        /// Gets or sets when it was last read from Cloudflare (UTC).
        /// </summary>
        public DateTime LastSeen { get; set; }
    }
}