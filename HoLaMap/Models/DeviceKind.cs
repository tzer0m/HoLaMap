namespace HoLaMap.Models
{
    /// <summary>
    /// The kind of a device.
    /// </summary>
    public enum DeviceKind
    {
        /// <summary>
        /// Router or gateway, e.g. the UDM-SE.
        /// </summary>
        Gateway,

        /// <summary>
        /// Network switch.
        /// </summary>
        Switch,

        /// <summary>
        /// Wireless access point.
        /// </summary>
        AccessPoint,

        /// <summary>
        /// Any other device seen on the network.
        /// </summary>
        Client
    }
}