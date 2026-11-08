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
        TrueNasApp,

        /// <summary>
        /// Anything else, added by hand.
        /// </summary>
        Other
    }
}