using System.Text.Json.Serialization;

namespace HoLaMap.UniFi.Models
{
    /// <summary>
    /// Uplink details of a UniFi device.
    /// </summary>
    public class UniFiUplink
    {
        /// <summary>
        /// Gets or sets the MAC address of the upstream device.
        /// </summary>
        [JsonPropertyName("uplink_mac")]
        public string? UplinkMac { get; set; }

        /// <summary>
        /// Gets or sets the port number on the upstream device.
        /// </summary>
        [JsonPropertyName("uplink_remote_port")]
        public int? UplinkRemotePort { get; set; }

        /// <summary>
        /// Gets or sets the local uplink port number.
        /// </summary>
        [JsonPropertyName("port_idx")]
        public int? PortIdx { get; set; }
    }
}