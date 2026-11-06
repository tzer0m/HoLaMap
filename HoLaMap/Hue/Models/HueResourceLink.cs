using System.Text.Json.Serialization;

namespace HoLaMap.Hue.Models
{
    /// <summary>
    /// A reference from one Hue resource to another.
    /// </summary>
    public class HueResourceLink
    {
        /// <summary>
        /// Gets or sets the linked resource's ID.
        /// </summary>
        [JsonPropertyName("rid")]
        public string Rid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the linked resource's type, e.g. light, motion or device_power.
        /// </summary>
        [JsonPropertyName("rtype")]
        public string Rtype { get; set; } = string.Empty;
    }
}