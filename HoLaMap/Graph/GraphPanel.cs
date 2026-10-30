namespace HoLaMap.Graph
{
    /// <summary>
    /// A blank or brush panel drawn in a rack.
    /// </summary>
    public class GraphPanel
    {
        /// <summary>
        /// The panel ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The kind of panel: blank, brush or patch.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// The name shown for a patch panel.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// How many ports a patch panel has, or 0 for other panels.
        /// </summary>
        public int Ports { get; set; }

        /// <summary>
        /// The cables plugged into a patch panel's ports.
        /// </summary>
        public List<GraphPatch> Patches { get; set; } = [];

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