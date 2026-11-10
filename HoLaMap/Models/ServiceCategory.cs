using System.ComponentModel.DataAnnotations;

namespace HoLaMap.Models
{
    /// <summary>
    /// What a service is for, which decides the tab it shows on.
    /// </summary>
    public enum ServiceCategory
    {
        /// <summary>
        /// Anything not given a category yet, which is where new services land.
        /// </summary>
        Other,

        /// <summary>
        /// Your own apps, including the Tzer ones.
        /// </summary>
        Custom,

        /// <summary>
        /// Someone else's app or tool that doesn't fit another category.
        /// </summary>
        [Display(Name = "3rd Party")]
        ThirdParty,

        /// <summary>
        /// A service in the cloud rather than on one of your devices.
        /// </summary>
        Cloud,

        /// <summary>
        /// Cloudflare: the tunnel connectors and Cloudflare's own services, like Turnstile.
        /// </summary>
        Cloudflare,

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