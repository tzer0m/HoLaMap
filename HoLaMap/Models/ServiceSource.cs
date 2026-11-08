namespace HoLaMap.Models
{
    /// <summary>
    /// Where a service came from.
    /// </summary>
    public enum ServiceSource
    {
        /// <summary>
        /// Reported by its host.
        /// </summary>
        Synced,

        /// <summary>
        /// Added by hand.
        /// </summary>
        Manual
    }
}