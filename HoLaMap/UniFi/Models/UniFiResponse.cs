using System.Text.Json.Serialization;

namespace HoLaMap.UniFi.Models
{
    /// <summary>
    /// Envelope returned by the classic UniFi Network API.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    public class UniFiResponse<T>
    {
        /// <summary>
        /// Gets or sets the returned records.
        /// </summary>
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = [];
    }
}