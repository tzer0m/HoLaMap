namespace HoLaMap.Graph
{
    /// <summary>
    /// A cable plugged into one side of a patch panel port.
    /// </summary>
    public class GraphPatch
    {
        /// <summary>
        /// The patch panel port number, counting from 1.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Which side of the port: front or back.
        /// </summary>
        public string Side { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the device at the other end, or empty if the cable goes to another patch panel port.
        /// </summary>
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the port at the other end, or empty if the cable goes to the device itself.
        /// </summary>
        public string Port { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the patch panel at the other end, or empty if the cable goes to a device.
        /// </summary>
        public string Panel { get; set; } = string.Empty;

        /// <summary>
        /// The port number at the other end, if the cable joins two patch panel ports.
        /// </summary>
        public int TargetNumber { get; set; }

        /// <summary>
        /// The side of the port at the other end: front or back.
        /// </summary>
        public string TargetSide { get; set; } = string.Empty;
    }
}