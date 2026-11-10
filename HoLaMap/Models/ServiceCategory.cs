using System.ComponentModel.DataAnnotations;

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
        /// Semaphore, running Ansible.
        /// </summary>
        Semaphore,

        /// <summary>
        /// A GitHub Actions runner.
        /// </summary>
        GitHub,

        /// <summary>
        /// The Asterisk phone system.
        /// </summary>
        Asterisk,

        /// <summary>
        /// The house: Home Assistant and the things it talks to.
        /// </summary>
        Home,

        /// <summary>
        /// Films, TV and the downloads feeding them.
        /// </summary>
        Media,

        /// <summary>
        /// Metrics, dashboards, uptime checks and the exporters they read.
        /// </summary>
        Monitoring,

        /// <summary>
        /// Documents, passwords, databases and the apps keeping records.
        /// </summary>
        [Display(Name = "Documents/Data")]
        Data
    }
}