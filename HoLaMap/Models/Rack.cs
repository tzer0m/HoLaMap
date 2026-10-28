namespace HoLaMap.Models
{
    /// <summary>
    /// A rack in a room. Devices sit in a grid of rack units tall and four columns wide.
    /// </summary>
    public class Rack
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the room identifier.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Gets or sets the room the rack is in.
        /// </summary>
        public Room Room { get; set; } = null!;

        /// <summary>
        /// Gets or sets the rack name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the height in rack units.
        /// </summary>
        public int HeightU { get; set; } = 12;

        /// <summary>
        /// Gets or sets the devices in the rack.
        /// </summary>
        public List<Device> Devices { get; set; } = [];
    }
}