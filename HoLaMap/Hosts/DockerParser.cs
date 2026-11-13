using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HoLaMap.Hosts.Models;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Reads the output of docker ps --format '{{json .}}'.
    /// </summary>
    public static class DockerParser
    {
        /// <summary>
        /// Reads every container. Each is a JSON object, whether one per line, spread over several lines, or inside an array. Reading stops at anything that isn't JSON.
        /// </summary>
        /// <param name="output">The output of docker ps.</param>
        /// <returns>The containers.</returns>
        public static List<DockerContainer> Parse(string output)
        {
            List<DockerContainer> containers = [];
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(output), new JsonReaderOptions { AllowMultipleValues = true });
            try
            {
                while (reader.Read())
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        continue;
                    }
                    DockerContainer? container = JsonSerializer.Deserialize<DockerContainer>(ref reader);
                    if (!string.IsNullOrEmpty(container?.Names))
                    {
                        containers.Add(container);
                    }
                }
            }
            catch (JsonException)
            {
                return containers;
            }
            return containers;
        }

        /// <summary>
        /// Gets each container as a service in its Compose project, leaving out one-off helpers like TrueNAS's permissions containers.
        /// </summary>
        /// <param name="output">The output of docker ps.</param>
        /// <param name="ignore">Compose service names to leave out.</param>
        /// <returns>The services.</returns>
        public static List<ReportedService> Services(string output, List<string> ignore)
        {
            return [.. Parse(output).Where(container => !ignore.Contains(Labels(container).GetValueOrDefault("com.docker.compose.service") ?? string.Empty, StringComparer.OrdinalIgnoreCase)).Select(container => new ReportedService { Name = Name(container), Group = Labels(container).GetValueOrDefault("com.docker.compose.project"), Ports = HostPorts(container) }).DistinctBy(service => (service.Group, service.Name)).OrderBy(service => service.Group).ThenBy(service => service.Name)];
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
        /// Gets the host ports a container is published on, from parts like 0.0.0.0:8082->8080/tcp, and every port of a small range like 0.0.0.0:8000-8002->8000-8002/tcp.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The host ports.</returns>
        private static List<int> HostPorts(DockerContainer container)
        {
            List<int> ports = [];
            foreach (Match match in Regex.Matches(container.Ports ?? string.Empty, @":(\d+)(?:-(\d+))?->"))
            {
                int first = int.Parse(match.Groups[1].Value);
                int last = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : first;
                if (last - first <= 100)
                {
                    ports.AddRange(Enumerable.Range(first, last - first + 1));
                }
            }
            return [.. ports.Distinct().Order()];
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