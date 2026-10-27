using System.Text.Json.Serialization;

namespace HoLaMap.UniFi.Models
{
    /// <summary>
    /// A port profile from rest/portconf.
    /// </summary>
    public class UniFiPortProfile
    {
        /// <summary>
        /// Gets or sets the profile ID.
        /// </summary>
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the profile name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the native network ID.
        /// </summary>
        [JsonPropertyName("native_networkconf_id")]
        public string? NativeNetworkId { get; set; }
    }
}