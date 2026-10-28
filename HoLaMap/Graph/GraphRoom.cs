namespace HoLaMap.Graph
{
    /// <summary>
    /// A room drawn on the floor plan.
    /// </summary>
    public class GraphRoom
    {
        /// <summary>
        /// The room ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The room name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The left edge in grid cells.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// The top edge in grid cells.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// The width in grid cells.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height in grid cells.
        /// </summary>
        public int Height { get; set; }
    }
}