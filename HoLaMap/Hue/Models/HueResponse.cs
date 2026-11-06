using System.Text.Json.Serialization;

namespace HoLaMap.Hue.Models
{
    /// <summary>
    /// The wrapper the Hue API puts round every list of resources.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    public class HueResponse<T>
    {
        /// <summary>
        /// Gets or sets the resources.
        /// </summary>
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = [];
    }
}