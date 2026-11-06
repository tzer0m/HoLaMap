using System.Text.Json.Serialization;

namespace HoLaMap.Hue.Models
{
    /// <summary>
    /// What a Hue device is, as its maker describes it.
    /// </summary>
    public class HueProductData
    {
        /// <summary>
        /// Gets or sets the manufacturer.
        /// </summary>
        [JsonPropertyName("manufacturer_name")]
        public string? ManufacturerName { get; set; }

        /// <summary>
        /// Gets or sets the product name, e.g. Hue color lamp.
        /// </summary>
        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }
    }
}