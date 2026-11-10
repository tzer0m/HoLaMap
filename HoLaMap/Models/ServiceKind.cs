using System.ComponentModel.DataAnnotations;

namespace HoLaMap.Models
{
    /// <summary>
    /// How a service runs on its host.
    /// </summary>
    public enum ServiceKind
    {
        /// <summary>
        /// A systemd unit.
        /// </summary>
        Systemd,

        /// <summary>
        /// A Docker container.
        /// </summary>
        Docker,

        /// <summary>
        /// An app on TrueNAS.
        /// </summary>
        [Display(Name = "TrueNAS App")]
        TrueNasApp,

        /// <summary>
        /// A Python script, added by hand.
        /// </summary>
        [Display(Name = "Python Script")]
        PythonScript,

        /// <summary>
        /// A first-level route of an API, read from its OpenAPI description.
        /// </summary>
        [Display(Name = "API Route")]
        ApiRoute,

        /// <summary>
        /// A service in the cloud rather than on one of your devices, added by hand.
        /// </summary>
        Cloud,

        /// <summary>
        /// A plain device that doesn't run anything, such as a printer, added by hand so other services can link to it.
        /// </summary>
        Device,

        /// <summary>
        /// Anything else, added by hand.
        /// </summary>
        Other
    }
}