namespace HoLaMap.Models
{
    /// <summary>
    /// The kind of a panel fitted in a rack.
    /// </summary>
    public enum RackPanelKind
    {
        /// <summary>
        /// A plain blanking plate covering empty units.
        /// </summary>
        Blank,

        /// <summary>
        /// A brush panel that cables pass through.
        /// </summary>
        Brush,

        /// <summary>
        /// A patch panel with an RJ45 socket on the front and back of each port.
        /// </summary>
        Patch
    }
}