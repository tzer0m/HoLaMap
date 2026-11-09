namespace HoLaMap.Hosts.Models
{
    /// <summary>
    /// An API whose routes are read from its OpenAPI description and shown as services.
    /// </summary>
    public class OpenApiSource
    {
        /// <summary>
        /// Gets or sets the name or MAC of the device the API runs on.
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API's service name on that host, which its routes are grouped under.
        /// </summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL of the OpenAPI description, e.g. its swagger.json.
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}