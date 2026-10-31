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

        /// <summary>
        /// Gets or sets the MAC address of the access point a wireless client is connected to.
        /// </summary>
        [JsonPropertyName("ap_mac")]
        public string? ApMac { get; set; }

        /// <summary>
        /// Gets or sets the name of the Wi-Fi network a wireless client is on.
        /// </summary>
        [JsonPropertyName("essid")]
        public string? Essid { get; set; }

        /// <summary>
        /// Gets or sets the radio a wireless client is on: ng for 2.4 GHz, na for 5 GHz or 6e for 6 GHz.
        /// </summary>
        [JsonPropertyName("radio")]
        public string? Radio { get; set; }

        /// <summary>
        /// Gets or sets the Wi-Fi channel.
        /// </summary>
        [JsonPropertyName("channel")]
        public int? Channel { get; set; }

        /// <summary>
        /// Gets or sets the signal strength in dBm.
        /// </summary>
        [JsonPropertyName("signal")]
        public int? Signal { get; set; }

        /// <summary>
        /// Gets or sets the rate the access point sends to the client at, in kbps.
        /// </summary>
        [JsonPropertyName("tx_rate")]
        public int? TxRate { get; set; }
    }
}