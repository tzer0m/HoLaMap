namespace HoLaMap.Models
{
    /// <summary>
    /// A floor of the building, holding rooms.
    /// </summary>
    public class Floor
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the floor name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the position of the floor in the list of floors.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets or sets the rooms on this floor.
        /// </summary>
        public List<Room> Rooms { get; set; } = [];
    }
}