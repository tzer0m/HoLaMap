using System.Text.Json.Serialization;

namespace HoLaMap.UniFi.Models
{
    /// <summary>
    /// A connected client from stat/sta.
    /// </summary>
    public class UniFiStation
    {
        /// <summary>
        /// Gets or sets the MAC address.
        /// </summary>
        [JsonPropertyName("mac")]
        public string Mac { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the alias set in UniFi.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the reported hostname.
        /// </summary>
        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        [JsonPropertyName("ip")]
        public string? Ip { get; set; }

        /// <summary>
        /// Gets or sets whether the client is wired.
        /// </summary>
        [JsonPropertyName("is_wired")]
        public bool IsWired { get; set; }

        /// <summary>
        /// Gets or sets the MAC address of the switch the client is plugged into.
        /// </summary>
        [JsonPropertyName("sw_mac")]
        public string? SwMac { get; set; }

        /// <summary>
        /// Gets or sets the switch port number.
        /// </summary>
        [JsonPropertyName("sw_port")]
        public int? SwPort { get; set; }
    }
}