namespace HoLaMap.Graph
{
    /// <summary>
    /// A rack drawn as a single block in place of the devices inside it.
    /// </summary>
    public class GraphRack
    {
        /// <summary>
        /// The rack ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The rack name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the room the rack is in.
        /// </summary>
        public string Room { get; set; } = string.Empty;

        /// <summary>
        /// Extra details shown when the rack is selected.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Whether any device in the rack was seen by the last few syncs, or the rack is empty.
        /// </summary>
        public bool Online { get; set; }
    }
}