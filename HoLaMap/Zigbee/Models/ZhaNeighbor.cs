using System.Text.Json.Serialization;

namespace HoLaMap.Zigbee.Models
{
    /// <summary>
    /// An entry in a Zigbee device's neighbour table, as ZHA reports it.
    /// </summary>
    public class ZhaNeighbor
    {
        /// <summary>
        /// Gets or sets the neighbour's IEEE address.
        /// </summary>
        [JsonPropertyName("ieee")]
        public string Ieee { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the link quality to the neighbour, from 0 to 255.
        /// </summary>
        [JsonPropertyName("lqi")]
        public int? Lqi { get; set; }

        /// <summary>
        /// Gets or sets how the neighbour is related: Parent, Child, Sibling, None or Previous_Child.
        /// </summary>
        [JsonPropertyName("relationship")]
        public string? Relationship { get; set; }
    }
}