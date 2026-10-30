namespace HoLaMap.Graph
{
    /// <summary>
    /// A blank or brush panel drawn in a rack.
    /// </summary>
    public class GraphPanel
    {
        /// <summary>
        /// The kind of panel: blank or brush.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// The lowest rack unit the panel occupies, counting from 1 at the bottom.
        /// </summary>
        public int RackU { get; set; }

        /// <summary>
        /// The left column the panel occupies, from 0 to 3.
        /// </summary>
        public int RackColumn { get; set; }

        /// <summary>
        /// How many of the rack's four columns the panel is wide.
        /// </summary>
        public int RackWidth { get; set; }

        /// <summary>
        /// How many rack units tall the panel is.
        /// </summary>
        public int RackHeight { get; set; }
    }
}