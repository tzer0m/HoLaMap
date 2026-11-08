namespace HoLaMap.Hue
{
    /// <summary>
    /// Settings for reading devices from a Philips Hue Bridge.
    /// </summary>
    public class HueOptions
    {
        /// <summary>
        /// Gets or sets the application key made by pressing the bridge's link button.
        /// </summary>
        public string AppKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the bridge's device on the map, which gives its IP address.
        /// </summary>
        public string BridgeHost { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of minutes between syncs.
        /// </summary>
        public int SyncIntervalMinutes { get; set; }

        /// <summary>
        /// Gets or sets how many days a device that has gone from the bridge is kept before it's forgotten, or 0 to keep it forever.
        /// </summary>
        public int ForgetAfterDays { get; set; } = 30;
    }
}