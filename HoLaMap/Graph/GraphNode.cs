namespace HoLaMap.Graph
{
    /// <summary>
    /// A device drawn on the map.
    /// </summary>
    public class GraphNode
    {
        /// <summary>
        /// The node ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The text shown on the node.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// The device kind in lowercase, used for styling.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// Extra details shown when the node is selected.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// The device's ports in order.
        /// </summary>
        public List<GraphPort> Ports { get; set; } = [];

        /// <summary>
        /// Whether the device was seen by the last few syncs.
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// The ID of the room the device is in, or empty if unplaced.
        /// </summary>
        public string Room { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the rack the device is in, or empty if it isn't in one.
        /// </summary>
        public string Rack { get; set; } = string.Empty;

        /// <summary>
        /// The device's IP address, or empty if it isn't known.
        /// </summary>
        public string Ip { get; set; } = string.Empty;

        /// <summary>
        /// The device model, or empty if it isn't known.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// The device's MAC address.
        /// </summary>
        public string Mac { get; set; } = string.Empty;

        /// <summary>
        /// When the device was last seen, in local time.
        /// </summary>
        public string LastSeen { get; set; } = string.Empty;

        /// <summary>
        /// The lowest rack unit the device occupies, counting from 1 at the bottom, or 0 if it isn't in a rack.
        /// </summary>
        public int RackU { get; set; }

        /// <summary>
        /// The left column the device occupies in its rack, from 0 to 3.
        /// </summary>
        public int RackColumn { get; set; }

        /// <summary>
        /// How many of the rack's four columns the device is wide.
        /// </summary>
        public int RackWidth { get; set; }

        /// <summary>
        /// How many rack units tall the device is.
        /// </summary>
        public int RackHeight { get; set; }
    }
}