using System.Text.Json.Serialization;

namespace HoLaMap.Hue.Models
{
    /// <summary>
    /// The name and kind given to a Hue device in the Hue app.
    /// </summary>
    public class HueMetadata
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the archetype, e.g. sultan_bulb or bridge_v2.
        /// </summary>
        [JsonPropertyName("archetype")]
        public string? Archetype { get; set; }
    }
}