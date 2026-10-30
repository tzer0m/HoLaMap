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
        /// The ID of the device at the other end.
        /// </summary>
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the port at the other end, or empty if the cable goes to the device itself.
        /// </summary>
        public string Port { get; set; } = string.Empty;
    }
}