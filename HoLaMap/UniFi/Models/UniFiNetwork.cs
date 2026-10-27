using System.Text.Json.Serialization;

namespace HoLaMap.UniFi.Models
{
    /// <summary>
    /// A network from rest/networkconf.
    /// </summary>
    public class UniFiNetwork
    {
        /// <summary>
        /// Gets or sets the network ID.
        /// </summary>
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the network name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the VLAN ID.
        /// </summary>
        [JsonPropertyName("vlan")]
        public int? Vlan { get; set; }

        /// <summary>
        /// Gets or sets the gateway address and prefix.
        /// </summary>
        [JsonPropertyName("ip_subnet")]
        public string? IpSubnet { get; set; }
    }
}