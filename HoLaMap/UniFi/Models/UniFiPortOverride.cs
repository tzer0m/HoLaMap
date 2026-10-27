using System.Text.Json.Serialization;

namespace HoLaMap.UniFi.Models
{
    /// <summary>
    /// A per-port configuration override on a UniFi device.
    /// </summary>
    public class UniFiPortOverride
    {
        /// <summary>
        /// Gets or sets the port number.
        /// </summary>
        [JsonPropertyName("port_idx")]
        public int PortIdx { get; set; }

        /// <summary>
        /// Gets or sets the native network ID.
        /// </summary>
        [JsonPropertyName("native_networkconf_id")]
        public string? NativeNetworkId { get; set; }

        /// <summary>
        /// Gets or sets the port profile ID.
        /// </summary>
        [JsonPropertyName("portconf_id")]
        public string? PortProfileId { get; set; }
    }
}