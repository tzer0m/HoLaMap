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

        /// <summary>
        /// Gets or sets how tagged VLANs are handled: auto allows all, block_all allows none, custom allows all but the excluded ones.
        /// </summary>
        [JsonPropertyName("tagged_vlan_mgmt")]
        public string? TaggedVlanMgmt { get; set; }

        /// <summary>
        /// Gets or sets the network IDs blocked from being tagged when tagged VLANs are custom.
        /// </summary>
        [JsonPropertyName("excluded_networkconf_ids")]
        public List<string>? ExcludedNetworkIds { get; set; }

        /// <summary>
        /// Gets or sets the tagged network IDs, as older firmware lists them.
        /// </summary>
        [JsonPropertyName("tagged_networkconf_ids")]
        public List<string>? TaggedNetworkIds { get; set; }
    }
}