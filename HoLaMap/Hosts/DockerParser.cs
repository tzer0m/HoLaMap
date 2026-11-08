using System.Text.Json;
using HoLaMap.Hosts.Models;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Reads the output of docker ps --format '{{json .}}'.
    /// </summary>
    public static class DockerParser
    {
        /// <summary>
        /// Reads every container, one JSON object per line. Lines that aren't JSON are skipped.
        /// </summary>
        /// <param name="output">The output of docker ps.</param>
        /// <returns>The containers.</returns>
        public static List<DockerContainer> Parse(string output)
        {
            List<DockerContainer> containers = [];
            foreach (string line in output.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                try
                {
                    DockerContainer? container = JsonSerializer.Deserialize<DockerContainer>(line);
                    if (!string.IsNullOrEmpty(container?.Names))
                    {
                        containers.Add(container);
                    }
                }
                catch (JsonException)
                {
                    continue;
                }
            }
            return containers;
        }

        /// <summary>
        /// Gets each container as a service, named by its container name.
        /// </summary>
        /// <param name="output">The output of docker ps.</param>
        /// <returns>The services.</returns>
        public static List<ReportedService> Services(string output)
        {
            return [.. Parse(output).DistinctBy(Name).OrderBy(Name).Select(container => new ReportedService { Name = Name(container), Group = Labels(container).GetValueOrDefault("com.docker.compose.project") })];
        }

        /// <summary>
        /// Gets a container's name. Compose's default name of project-service-number is shortened to just the service.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The name.</returns>
        private static string Name(DockerContainer container)
        {
            string name = container.Names!.Split(',')[0].Trim();
            Dictionary<string, string> labels = Labels(container);
            bool compose = labels.TryGetValue("com.docker.compose.project", out string? project) & labels.TryGetValue("com.docker.compose.service", out string? service) & labels.TryGetValue("com.docker.compose.container-number", out string? number);
            return compose && name == $"{project}-{service}-{number}" ? service! : name;
        }

        /// <summary>
        /// Reads a container's labels from their key=value pairs.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The labels by key.</returns>
        private static Dictionary<string, string> Labels(DockerContainer container)
        {
            Dictionary<string, string> labels = [];
            foreach (string pair in (container.Labels ?? string.Empty).Split(','))
            {
                int equals = pair.IndexOf('=');
                if (equals > 0)
                {
                    labels[pair[..equals]] = pair[(equals + 1)..];
                }
            }
            return labels;
        }
    }
}