using System.Text.RegularExpressions;
using HoLaMap.Hosts.Models;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Reads the output of ss -tlnpH.
    /// </summary>
    public static class PortsParser
    {
        /// <summary>
        /// Reads each listening socket's port and the first process holding it, from lines like LISTEN 0 512 127.0.0.1:5152 0.0.0.0:* users:(("PourCast",pid=1234,fd=230)).
        /// </summary>
        /// <param name="output">The output of ss.</param>
        /// <returns>The ports, once each per process.</returns>
        public static List<ListeningPort> Parse(string output)
        {
            List<ListeningPort> ports = [];
            foreach (string line in output.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Match process = Regex.Match(line, "\"([^\"]+)\",pid=");
                int colon = columns.Length > 3 ? columns[3].LastIndexOf(':') : -1;
                if (process.Success && colon >= 0 && int.TryParse(columns[3][(colon + 1)..], out int port))
                {
                    ports.Add(new ListeningPort { Process = process.Groups[1].Value, Port = port });
                }
            }
            return [.. ports.DistinctBy(item => (item.Process, item.Port))];
        }
    }
}