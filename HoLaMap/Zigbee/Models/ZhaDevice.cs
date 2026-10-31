using System.Text.Json.Serialization;

namespace HoLaMap.Zigbee.Models
{
    /// <summary>
    /// A Zigbee device as ZHA's zha/devices command reports it.
    /// </summary>
    public class ZhaDevice
    {
        /// <summary>
        /// Gets or sets the IEEE address.
        /// </summary>
        [JsonPropertyName("ieee")]
        public string Ieee { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name ZHA built from the manufacturer and model.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the name given in Home Assistant, if any.
        /// </summary>
        [JsonPropertyName("user_given_name")]
        public string? UserGivenName { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer.
        /// </summary>
        [JsonPropertyName("manufacturer")]
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the role: Coordinator, Router or EndDevice.
        /// </summary>
        [JsonPropertyName("device_type")]
        public string? DeviceType { get; set; }

        /// <summary>
        /// Gets or sets the power source, e.g. Mains or Battery or Unknown.
        /// </summary>
        [JsonPropertyName("power_source")]
        public string? PowerSource { get; set; }

        /// <summary>
        /// Gets or sets the last link quality seen, from 0 to 255.
        /// </summary>
        [JsonPropertyName("lqi")]
        public int? Lqi { get; set; }

        /// <summary>
        /// Gets or sets the last signal strength in dBm.
        /// </summary>
        [JsonPropertyName("rssi")]
        public int? Rssi { get; set; }

        /// <summary>
        /// Gets or sets when the device was last heard from.
        /// </summary>
        [JsonPropertyName("last_seen")]
        public string? LastSeen { get; set; }

        /// <summary>
        /// Gets or sets whether ZHA considers the device available.
        /// </summary>
        [JsonPropertyName("available")]
        public bool Available { get; set; }

        /// <summary>
        /// Gets or sets the device's neighbour table.
        /// </summary>
        [JsonPropertyName("neighbors")]
        public List<ZhaNeighbor> Neighbors { get; set; } = [];
    }
}