namespace HoLaMap.Models
{
    /// <summary>
    /// Where a link came from.
    /// </summary>
    public enum LinkSource
    {
        /// <summary>
        /// Detected by the UniFi sync.
        /// </summary>
        UniFi,

        /// <summary>
        /// Added by hand.
        /// </summary>
        Manual
    }
}