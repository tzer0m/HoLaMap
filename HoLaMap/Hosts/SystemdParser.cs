using HoLaMap.Hosts.Models;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Reads the output of systemctl show.
    /// </summary>
    public static class SystemdParser
    {
        /// <summary>
        /// Where units set up by hand live, as opposed to ones that came with the OS or a package.
        /// </summary>
        private const string LocalUnitPath = "/etc/systemd/system/";

        /// <summary>
        /// Reads every unit, where each one is a block of Key=Value lines followed by a blank line.
        /// </summary>
        /// <param name="output">The output of systemctl show.</param>
        /// <returns>The units.</returns>
        public static List<SystemdUnit> Parse(string output)
        {
            List<SystemdUnit> units = [];
            foreach (string block in output.ReplaceLineEndings("\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
            {
                Dictionary<string, string> values = [];
                foreach (string line in block.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    int equals = line.IndexOf('=');
                    if (equals > 0)
                    {
                        values[line[..equals]] = line[(equals + 1)..];
                    }
                }
                if (values.TryGetValue("Id", out string? id))
                {
                    units.Add(new SystemdUnit { Id = id, Description = values.GetValueOrDefault("Description"), FragmentPath = values.GetValueOrDefault("FragmentPath"), ActiveState = values.GetValueOrDefault("ActiveState"), SubState = values.GetValueOrDefault("SubState") });
                }
            }
            return units;
        }

        /// <summary>
        /// Gets the names of the services set up by hand, plus any packaged ones asked for, without the .service suffix.
        /// </summary>
        /// <param name="output">The output of systemctl show.</param>
        /// <param name="include">Packaged services to keep as well.</param>
        /// <returns>The service names.</returns>
        public static List<string> ServiceNames(string output, List<string> include)
        {
            return [.. Parse(output).Where(unit => unit.FragmentPath?.StartsWith(LocalUnitPath, StringComparison.Ordinal) == true || (!string.IsNullOrEmpty(unit.FragmentPath) && include.Contains(Name(unit), StringComparer.OrdinalIgnoreCase))).Select(Name).Distinct().OrderBy(name => name)];
        }

        /// <summary>
        /// Gets a unit's name without the .service suffix.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <returns>The name.</returns>
        private static string Name(SystemdUnit unit) => unit.Id.EndsWith(".service", StringComparison.Ordinal) ? unit.Id[..^".service".Length] : unit.Id;
    }
}