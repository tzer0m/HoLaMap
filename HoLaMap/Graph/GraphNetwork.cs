namespace HoLaMap.Graph
{
    /// <summary>
    /// A network (VLAN) that can be picked on the VLANs view.
    /// </summary>
    public class GraphNetwork
    {
        /// <summary>
        /// The network name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The VLAN ID, or null for the untagged default network.
        /// </summary>
        public int? VlanId { get; set; }

        /// <summary>
        /// The subnet in CIDR form, such as 192.168.10.1/24, or empty if it has none.
        /// </summary>
        public string Subnet { get; set; } = string.Empty;
    }
}