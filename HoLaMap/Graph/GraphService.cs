namespace HoLaMap.Graph
{
    /// <summary>
    /// A service drawn on the Services view.
    /// </summary>
    public class GraphService
    {
        /// <summary>
        /// The service ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The service name.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// The full name the host reports, when the label shortens it.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// How it runs, e.g. Systemd.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// What it's for, e.g. App.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Whether it was added by hand.
        /// </summary>
        public bool Manual { get; set; }

        /// <summary>
        /// Whether it's one of your own apps rather than third-party software.
        /// </summary>
        public bool Mine { get; set; }

        /// <summary>
        /// Whether it was in its host's latest report, or was added by hand.
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// When its host last reported it.
        /// </summary>
        public string LastSeen { get; set; } = string.Empty;
    }
}