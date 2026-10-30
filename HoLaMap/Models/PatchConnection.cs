namespace HoLaMap.Models
{
    /// <summary>
    /// A cable plugged into one side of a patch panel port, running to a device, one of its ports, or another patch panel port.
    /// </summary>
    public class PatchConnection
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the patch panel.
        /// </summary>
        public int RackPanelId { get; set; }

        /// <summary>
        /// Gets or sets the patch panel.
        /// </summary>
        public RackPanel RackPanel { get; set; } = null!;

        /// <summary>
        /// Gets or sets the patch panel port number, counting from 1.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets which side of the port the cable plugs into.
        /// </summary>
        public PatchSide Side { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the device at the other end, or null if the cable goes to another patch panel port.
        /// </summary>
        public int? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the device at the other end, or null if the cable goes to another patch panel port.
        /// </summary>
        public Device? Device { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the port at the other end, if the device has ports.
        /// </summary>
        public int? PortId { get; set; }

        /// <summary>
        /// Gets or sets the port at the other end, if the device has ports.
        /// </summary>
        public Port? Port { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the patch panel at the other end, if the cable joins two patch panel ports.
        /// </summary>
        public int? TargetPanelId { get; set; }

        /// <summary>
        /// Gets or sets the patch panel at the other end, if the cable joins two patch panel ports.
        /// </summary>
        public RackPanel? TargetPanel { get; set; }

        /// <summary>
        /// Gets or sets the port number at the other end, if the cable joins two patch panel ports.
        /// </summary>
        public int? TargetNumber { get; set; }

        /// <summary>
        /// Gets or sets the side of the port at the other end, if the cable joins two patch panel ports.
        /// </summary>
        public PatchSide? TargetSide { get; set; }
    }
}