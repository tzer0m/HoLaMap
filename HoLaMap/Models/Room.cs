namespace HoLaMap.Models
{
    /// <summary>
    /// A room on a floor, drawn as a rectangle on the floor plan.
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the owning floor identifier.
        /// </summary>
        public int FloorId { get; set; }

        /// <summary>
        /// Gets or sets the owning floor.
        /// </summary>
        public Floor Floor { get; set; } = null!;

        /// <summary>
        /// Gets or sets the room name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the left edge on the floor plan in grid cells.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the top edge on the floor plan in grid cells.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Gets or sets the width on the floor plan in grid cells.
        /// </summary>
        public int Width { get; set; } = 6;

        /// <summary>
        /// Gets or sets the height on the floor plan in grid cells.
        /// </summary>
        public int Height { get; set; } = 4;

        /// <summary>
        /// Gets or sets the devices in this room.
        /// </summary>
        public List<Device> Devices { get; set; } = [];
    }
}