using System.Text.Json.Serialization;

namespace HoLaMap.Hosts.Models
{
    /// <summary>
    /// A container as reported by docker ps with its JSON format.
    /// </summary>
    public class DockerContainer
    {
        /// <summary>
        /// Gets or sets the container ID.
        /// </summary>
        [JsonPropertyName("ID")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the container names, separated by commas.
        /// </summary>
        [JsonPropertyName("Names")]
        public string? Names { get; set; }

        /// <summary>
        /// Gets or sets the image it runs.
        /// </summary>
        [JsonPropertyName("Image")]
        public string? Image { get; set; }

        /// <summary>
        /// Gets or sets the state, e.g. running or exited.
        /// </summary>
        [JsonPropertyName("State")]
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the status, e.g. Up 3 days.
        /// </summary>
        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the labels as key=value pairs separated by commas.
        /// </summary>
        [JsonPropertyName("Labels")]
        public string? Labels { get; set; }
    }
}