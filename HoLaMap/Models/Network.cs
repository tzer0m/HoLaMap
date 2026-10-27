namespace HoLaMap.Models
{
    /// <summary>
    /// A network (VLAN) and its subnet.
    /// </summary>
    public class Network
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the network name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the VLAN ID, if tagged.
        /// </summary>
        public int? VlanId { get; set; }

        /// <summary>
        /// Gets or sets the gateway address and prefix, e.g. 192.168.10.1/24.
        /// </summary>
        public string? Subnet { get; set; }
    }
}