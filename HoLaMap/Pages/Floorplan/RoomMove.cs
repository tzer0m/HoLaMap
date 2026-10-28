namespace HoLaMap.Pages.Floorplan
{
    /// <summary>
    /// A room's new position and size, sent when it is dragged or resized.
    /// </summary>
    public class RoomMove
    {
        /// <summary>
        /// The room identifier.
        /// </summary>
        public int Id { get; set; }

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