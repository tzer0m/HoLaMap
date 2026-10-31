using HoLaMap.UniFi.Models;

namespace HoLaMap.UniFi
{
    /// <summary>
    /// Works out a switch port's native and tagged networks from its own settings, overrides and port profile.
    /// </summary>
    /// <param name="networks">All networks.</param>
    /// <param name="profiles">All port profiles.</param>
    public class NetworkResolver(List<UniFiNetwork> networks, List<UniFiPortProfile> profiles)
    {
        /// <summary>
        /// Network names keyed by network ID.
        /// </summary>
        private readonly Dictionary<string, string> NetworkNames = networks.Where(network => network.Id != null && network.Name != null).ToDictionary(network => network.Id!, network => network.Name!);

        /// <summary>
        /// Port profiles keyed by profile ID.
        /// </summary>
        private readonly Dictionary<string, UniFiPortProfile> Profiles = profiles.Where(profile => profile.Id != null).ToDictionary(profile => profile.Id!);

        /// <summary>
        /// The untagged LAN used when a port has no native network set.
        /// </summary>
        private readonly string? DefaultNetwork = networks.FirstOrDefault(network => network.Vlan == null && network.IpSubnet != null)?.Name;

        /// <summary>
        /// The IDs of the networks that have a VLAN, which are the ones a port can carry tagged.
        /// </summary>
        private readonly List<string> VlanIds = [.. networks.Where(network => network.Id != null && network.Name != null && network.Vlan != null).Select(network => network.Id!)];

        /// <summary>
        /// Resolves the native network name for a port.
        /// </summary>
        /// <param name="port">The port from the port table.</param>
        /// <param name="portOverride">The device's override for this port, if any.</param>
        /// <returns>The network name, or null if it cannot be worked out.</returns>
        public string? Resolve(UniFiPort port, UniFiPortOverride? portOverride)
        {
            if (!string.IsNullOrEmpty(port.NetworkName))
            {
                return port.NetworkName.Equals(port.NetworkName, StringComparison.InvariantCultureIgnoreCase) ? port.NetworkName.ToUpperInvariant() : port.NetworkName;
            }
            string? profileId = portOverride?.PortProfileId ?? port.PortProfileId;
            string? networkId = port.NativeNetworkId ?? portOverride?.NativeNetworkId ?? (profileId != null && Profiles.TryGetValue(profileId, out UniFiPortProfile? profile) ? profile.NativeNetworkId : null);
            if (networkId != null && NetworkNames.TryGetValue(networkId, out string? name))
            {
                return name;
            }
            return DefaultNetwork;
        }

        /// <summary>
        /// Resolves the networks a port carries tagged, from its override or else its port profile. Ports allow every VLAN tagged unless told otherwise.
        /// </summary>
        /// <param name="port">The port from the port table.</param>
        /// <param name="portOverride">The device's override for this port, if any.</param>
        /// <param name="native">The port's native network, which is never also tagged.</param>
        /// <returns>The names of the tagged networks.</returns>
        public List<string> ResolveTagged(UniFiPort port, UniFiPortOverride? portOverride, string? native)
        {
            string? profileId = portOverride?.PortProfileId ?? port.PortProfileId;
            UniFiPortProfile? profile = profileId != null && Profiles.TryGetValue(profileId, out UniFiPortProfile? found) ? found : null;
            string mode = portOverride?.TaggedVlanMgmt ?? profile?.TaggedVlanMgmt ?? "auto";
            List<string>? listed = portOverride?.TaggedNetworkIds ?? profile?.TaggedNetworkIds;
            List<string> excluded = portOverride?.ExcludedNetworkIds ?? profile?.ExcludedNetworkIds ?? [];
            IEnumerable<string> ids = mode switch { "block_all" => [], "custom" => VlanIds.Where(id => !excluded.Contains(id)), _ => listed ?? VlanIds };
            return [.. ids.Where(id => NetworkNames.ContainsKey(id)).Select(id => NetworkNames[id]).Where(name => !string.Equals(name, native, StringComparison.OrdinalIgnoreCase)).Distinct().OrderBy(name => name)];
        }
    }
}