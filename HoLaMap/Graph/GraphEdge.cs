namespace HoLaMap.Graph
{
    /// <summary>
    /// A link drawn on the map.
    /// </summary>
    public class GraphEdge
    {
        /// <summary>
        /// The edge ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The upstream node ID.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// The downstream node ID.
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// The upstream port ID the line leaves from.
        /// </summary>
        public string SourcePort { get; set; } = string.Empty;

        /// <summary>
        /// The downstream port ID, if known.
        /// </summary>
        public string TargetPort { get; set; } = string.Empty;

        /// <summary>
        /// The port numbers shown on the edge.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// The port CSS class, used for the edge colour.
        /// </summary>
        public string Style { get; set; } = string.Empty;

        /// <summary>
        /// Whether the link was added by hand.
        /// </summary>
        public bool Manual { get; set; }

        /// <summary>
        /// Whether the upstream port is supplying PoE power over this link.
        /// </summary>
        public bool PoE { get; set; }

        /// <summary>
        /// Whether a PoE splitter sits inline on the cable, just before the target device.
        /// </summary>
        public bool Splitter { get; set; }

        /// <summary>
        /// Whether this is a Wi-Fi connection from an access point rather than a cable.
        /// </summary>
        public bool Wireless { get; set; }

        /// <summary>
        /// Notes added to a link by hand.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// The Wi-Fi network name, for a Wi-Fi link.
        /// </summary>
        public string Ssid { get; set; } = string.Empty;

        /// <summary>
        /// The Wi-Fi band, such as 5 GHz, for a Wi-Fi link.
        /// </summary>
        public string Band { get; set; } = string.Empty;

        /// <summary>
        /// The Wi-Fi channel, for a Wi-Fi link.
        /// </summary>
        public int? Channel { get; set; }

        /// <summary>
        /// The Wi-Fi signal strength in dBm, for a Wi-Fi link.
        /// </summary>
        public int? Signal { get; set; }

        /// <summary>
        /// The Wi-Fi link rate in Mbps, for a Wi-Fi link.
        /// </summary>
        public int? Rate { get; set; }

        /// <summary>
        /// Whether the downstream device was seen by the last few syncs.
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// Extra details shown when the edge is selected.
        /// </summary>
        public string Detail { get; set; } = string.Empty;
    }
}