using System.Text.Json.Serialization;

namespace HoLaMap.UniFi.Models
{
    /// <summary>
    /// A UniFi device (gateway, switch or access point) from stat/device.
    /// </summary>
    public class UniFiDevice
    {
        /// <summary>
        /// Gets or sets the MAC address.
        /// </summary>
        [JsonPropertyName("mac")]
        public string Mac { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the device name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the model code.
        /// </summary>
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the device type (udm, usw, uap).
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        [JsonPropertyName("ip")]
        public string? Ip { get; set; }

        /// <summary>
        /// Gets or sets the port table.
        /// </summary>
        [JsonPropertyName("port_table")]
        public List<UniFiPort> PortTable { get; set; } = [];

        /// <summary>
        /// Gets or sets the uplink details.
        /// </summary>
        [JsonPropertyName("uplink")]
        public UniFiUplink? Uplink { get; set; }
    }
}