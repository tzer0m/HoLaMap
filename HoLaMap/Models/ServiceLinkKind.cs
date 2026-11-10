using System.ComponentModel.DataAnnotations;

namespace HoLaMap.Models
{
    /// <summary>
    /// What one service uses another for, which decides how the link is drawn.
    /// </summary>
    public enum ServiceLinkKind
    {
        /// <summary>
        /// Any other use, drawn as a plain grey line.
        /// </summary>
        Other,

        /// <summary>
        /// The service stores its data in the other.
        /// </summary>
        Database,

        /// <summary>
        /// The service signs people in through the other.
        /// </summary>
        [Display(Name = "Sign-in")]
        SignIn,

        /// <summary>
        /// The service sends notifications through the other.
        /// </summary>
        Notification,

        /// <summary>
        /// The service calls the other's API.
        /// </summary>
        [Display(Name = "API")]
        Api,

        /// <summary>
        /// The service checks or shows the other's status.
        /// </summary>
        Monitoring,

        /// <summary>
        /// The service plays, fetches or manages media through the other.
        /// </summary>
        Media,

        /// <summary>
        /// The service controls a device, such as a printer or speaker.
        /// </summary>
        Device,

        /// <summary>
        /// The service sends email through the other.
        /// </summary>
        Email
    }
}