using System.Text.Json.Serialization;

namespace HoLaMap.Hue.Models
{
    /// <summary>
    /// A device on the Hue Bridge.
    /// </summary>
    public class HueDevice
    {
        /// <summary>
        /// Gets or sets the device's resource ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets what the device is.
        /// </summary>
        [JsonPropertyName("product_data")]
        public HueProductData ProductData { get; set; } = new();

        /// <summary>
        /// Gets or sets the device's name and kind.
        /// </summary>
        [JsonPropertyName("metadata")]
        public HueMetadata Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets the services the device offers, such as a light, a motion sensor or a battery.
        /// </summary>
        [JsonPropertyName("services")]
        public List<HueResourceLink> Services { get; set; } = [];
    }
}