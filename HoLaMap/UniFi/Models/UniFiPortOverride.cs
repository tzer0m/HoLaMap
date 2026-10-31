using System.Text.Json.Serialization;

namespace HoLaMap.UniFi.Models
{
    /// <summary>
    /// A per-port configuration override on a UniFi device.
    /// </summary>
    public class UniFiPortOverride
    {
        /// <summary>
        /// Gets or sets the port number.
        /// </summary>
        [JsonPropertyName("port_idx")]
        public int PortIdx { get; set; }

        /// <summary>
        /// Gets or sets the native network ID.
        /// </summary>
        [JsonPropertyName("native_networkconf_id")]
        public string? NativeNetworkId { get; set; }

        /// <summary>
        /// Gets or sets the port profile ID.
        /// </summary>
        [JsonPropertyName("portconf_id")]
        public string? PortProfileId { get; set; }

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