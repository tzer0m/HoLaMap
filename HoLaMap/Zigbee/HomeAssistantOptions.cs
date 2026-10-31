namespace HoLaMap.Zigbee
{
    /// <summary>
    /// Settings for reading Zigbee devices from Home Assistant's ZHA integration.
    /// </summary>
    public class HomeAssistantOptions
    {
        /// <summary>
        /// Gets or sets the Home Assistant base URL, e.g. http://192.168.20.132:8123/.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the long-lived access token.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of minutes between syncs.
        /// </summary>
        public int SyncIntervalMinutes { get; set; }

        /// <summary>
        /// Gets or sets the name of the device the Zigbee coordinator is plugged into, so the mesh hangs off it on the map.
        /// </summary>
        public string CoordinatorHost { get; set; } = string.Empty;
    }
}