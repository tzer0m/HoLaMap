using System.Text.Json.Serialization;

namespace HoLaMap.UniFi.Models
{
    /// <summary>
    /// A port in a UniFi device's port table.
    /// </summary>
    public class UniFiPort
    {
        /// <summary>
        /// Gets or sets the port number.
        /// </summary>
        [JsonPropertyName("port_idx")]
        public int PortIdx { get; set; }

        /// <summary>
        /// Gets or sets the port name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the media type (FE, GE, SFP+ etc.).
        /// </summary>
        [JsonPropertyName("media")]
        public string? Media { get; set; }

        /// <summary>
        /// Gets or sets the current speed in Mbps.
        /// </summary>
        [JsonPropertyName("speed")]
        public int? Speed { get; set; }

        /// <summary>
        /// Gets or sets whether the link is up.
        /// </summary>
        [JsonPropertyName("up")]
        public bool Up { get; set; }

        /// <summary>
        /// Gets or sets whether the port supports PoE.
        /// </summary>
        [JsonPropertyName("port_poe")]
        public bool PortPoe { get; set; }

        /// <summary>
        /// Gets or sets the PoE power currently being drawn in watts.
        /// </summary>
        [JsonPropertyName("poe_power")]
        public double? PoePower { get; set; }

        /// <summary>
        /// Gets or sets whether this is the uplink port.
        /// </summary>
        [JsonPropertyName("is_uplink")]
        public bool IsUplink { get; set; }

        /// <summary>
        /// Gets or sets the assigned network name.
        /// </summary>
        [JsonPropertyName("network_name")]
        public string? NetworkName { get; set; }
    }
}