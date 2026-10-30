namespace HoLaMap.Models
{
    /// <summary>
    /// A blank or brush panel fitted in a rack.
    /// </summary>
    public class RackPanel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the rack the panel is in.
        /// </summary>
        public int RackId { get; set; }

        /// <summary>
        /// Gets or sets the rack the panel is in.
        /// </summary>
        public Rack Rack { get; set; } = null!;

        /// <summary>
        /// Gets or sets the kind of panel.
        /// </summary>
        public RackPanelKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the lowest rack unit the panel occupies, counting from 1 at the bottom.
        /// </summary>
        public int RackU { get; set; } = 1;

        /// <summary>
        /// Gets or sets the left column the panel occupies, from 0 to 3.
        /// </summary>
        public int RackColumn { get; set; }

        /// <summary>
        /// Gets or sets how many of the four columns the panel is wide: 1, 2 or 4.
        /// </summary>
        public int RackWidth { get; set; } = 4;

        /// <summary>
        /// Gets or sets how many rack units tall the panel is.
        /// </summary>
        public int RackHeight { get; set; } = 1;
    }
}