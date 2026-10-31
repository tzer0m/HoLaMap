namespace HoLaMap.Models
{
    /// <summary>
    /// A port on a device.
    /// </summary>
    public class Port
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the owning device identifier.
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the owning device.
        /// </summary>
        public Device Device { get; set; } = null!;

        /// <summary>
        /// Gets or sets the port number on the device.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the port name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the maximum supported speed in Mbps.
        /// </summary>
        public int? MaxSpeed { get; set; }

        /// <summary>
        /// Gets or sets the current link speed in Mbps.
        /// </summary>
        public int? Speed { get; set; }

        /// <summary>
        /// Gets or sets whether the port supports PoE.
        /// </summary>
        public bool PoE { get; set; }

        /// <summary>
        /// Gets or sets the PoE power currently being drawn in watts.
        /// </summary>
        public double? PoEPower { get; set; }

        /// <summary>
        /// Gets or sets whether the link is up.
        /// </summary>
        public bool Up { get; set; }

        /// <summary>
        /// Gets or sets whether this is the device's uplink port.
        /// </summary>
        public bool IsUplink { get; set; }

        /// <summary>
        /// Gets or sets the name of the network (VLAN) assigned to the port.
        /// </summary>
        public string? NetworkName { get; set; }

        /// <summary>
        /// Gets or sets whether several devices can share this port, e.g. behind an unmanaged switch.
        /// </summary>
        public bool Shared { get; set; }

        /// <summary>
        /// Gets or sets the names of the networks (VLANs) the port carries tagged.
        /// </summary>
        public List<string> TaggedNetworks { get; set; } = [];
    }
}