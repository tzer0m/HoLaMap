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
        Notification
    }
}