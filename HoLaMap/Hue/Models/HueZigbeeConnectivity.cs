using System.Text.Json.Serialization;

namespace HoLaMap.Hue.Models
{
    /// <summary>
    /// A Hue device's Zigbee radio and whether the bridge can reach it.
    /// </summary>
    public class HueZigbeeConnectivity
    {
        /// <summary>
        /// Gets or sets the device the radio belongs to.
        /// </summary>
        [JsonPropertyName("owner")]
        public HueResourceLink Owner { get; set; } = new();

        /// <summary>
        /// Gets or sets the status: connected, disconnected, connectivity_issue or unidirectional_incoming.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the radio's IEEE address.
        /// </summary>
        [JsonPropertyName("mac_address")]
        public string? MacAddress { get; set; }
    }
}