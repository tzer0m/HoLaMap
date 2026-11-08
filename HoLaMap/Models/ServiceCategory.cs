namespace HoLaMap.Models
{
    /// <summary>
    /// What a service is for, which decides the tab it shows on.
    /// </summary>
    public enum ServiceCategory
    {
        /// <summary>
        /// An app or anything not in another category.
        /// </summary>
        App,

        /// <summary>
        /// A Cloudflare Tunnel connector.
        /// </summary>
        Cloudflared,

        /// <summary>
        /// A Prometheus exporter.
        /// </summary>
        Exporter,

        /// <summary>
        /// Semaphore, running Ansible.
        /// </summary>
        Semaphore,

        /// <summary>
        /// A Tailscale node.
        /// </summary>
        Tailscale,

        /// <summary>
        /// A GitHub Actions runner.
        /// </summary>
        GitHub
    }
}