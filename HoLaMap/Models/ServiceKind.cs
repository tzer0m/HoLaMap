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
        /// Anything else, added by hand.
        /// </summary>
        Other
    }
}