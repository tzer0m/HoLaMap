namespace HoLaMap.Graph
{
    /// <summary>
    /// A dependency drawn on the Services view, from the service that uses the other to the one it uses.
    /// </summary>
    public class GraphServiceLink
    {
        /// <summary>
        /// The link ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the service that uses the other.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the service being used.
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// What the service is used for, e.g. Other.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// Optional notes about the link.
        /// </summary>
        public string Notes { get; set; } = string.Empty;
    }
}