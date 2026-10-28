namespace HoLaMap.Graph
{
    /// <summary>
    /// A floor drawn on the map, holding rooms.
    /// </summary>
    public class GraphFloor
    {
        /// <summary>
        /// The floor ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The floor name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The rooms on the floor.
        /// </summary>
        public List<GraphRoom> Rooms { get; set; } = [];
    }
}