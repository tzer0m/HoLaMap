namespace HoLaMap.Models
{
    /// <summary>
    /// The Zigbee network a device belongs to.
    /// </summary>
    public enum ZigbeeNetwork
    {
        /// <summary>
        /// The ZHA network in Home Assistant.
        /// </summary>
        Zha,

        /// <summary>
        /// The Hue Bridge's own network.
        /// </summary>
        Hue
    }
}