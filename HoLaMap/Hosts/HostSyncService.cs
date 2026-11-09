using HoLaMap.Data;
using HoLaMap.Hosts.Models;
using HoLaMap.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Saves the services a host reports. Services added by hand are left alone.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="options">The host report settings, read fresh for each report so changes to the categories apply without a restart.</param>
    public class HostSyncService(HoLaMapContext context, IOptionsSnapshot<HostReportOptions> options)
    {
        /// <summary>
        /// Updates one kind of synced service on a host to match what it just reported.
        /// </summary>
        /// <param name="host">The host's device name or MAC address.</param>
        /// <param name="kind">The kind of service reported.</param>
        /// <param name="reported">The services running.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scope">The one group the report covers, such as one API's routes, or null if it covers every service of its kind on the host.</param>
        /// <returns>The number of services reported, or null if no device matches the host.</returns>
        public async Task<int?> SyncAsync(string host, ServiceKind kind, List<ReportedService> reported, CancellationToken cancellationToken, string? scope = null)
        {
            string name = host.Trim().ToLowerInvariant();
            string mac = name.Replace('-', ':');
            Device? device = await context.Devices.Include(item => item.Services).FirstOrDefaultAsync(item => item.Name.ToLower() == name || item.Mac == mac, cancellationToken);
            if (device == null)
            {
                return null;
            }
            DateTime now = DateTime.UtcNow;
            // A service is known by its group and name, since containers in different Compose projects can share a name like postgres.
            HashSet<(string, string)> keys = [.. reported.Select(item => (item.Group ?? string.Empty, item.Name))];

            // A service that has gone from its host stays greyed out in case it comes back, and is forgotten once it hasn't been reported for a while.
            int forgetAfterDays = options.Value.ForgetAfterDays;
            foreach (Service missing in device.Services.Where(service => service.Kind == kind && service.Source == ServiceSource.Synced && (scope == null || service.Group == scope) && !keys.Contains((service.Group ?? string.Empty, service.Name))).ToList())
            {
                if (forgetAfterDays > 0 && missing.LastSeen < now.AddDays(-forgetAfterDays))
                {
                    context.Services.Remove(missing);
                }
            }
            foreach (ReportedService item in reported)
            {
                Service? service = device.Services.FirstOrDefault(existing => existing.Kind == kind && existing.Name == item.Name && (existing.Group ?? string.Empty) == (item.Group ?? string.Empty));
                if (service == null)
                {
                    service = new Service { Name = item.Name, Kind = kind, Source = ServiceSource.Synced, Group = item.Group };
                    device.Services.Add(service);
                }
                if (service.Source == ServiceSource.Synced)
                {
                    // It's one of your own apps if it was set up by hand and isn't a known tool with its own category.
                    service.LastSeen = now;
                    service.Category = CategoryOf(item.Name);
                    service.Mine = item.Custom && service.Category == ServiceCategory.App;
                }
            }
            await context.SaveChangesAsync(cancellationToken);
            return keys.Count;
        }

        /// <summary>
        /// Works out a service's category from the settings, where a trailing * matches the start of the name.
        /// </summary>
        /// <param name="name">The service name.</param>
        /// <returns>The category, or App if none matches.</returns>
        private ServiceCategory CategoryOf(string name)
        {
            foreach (KeyValuePair<string, ServiceCategory> pair in options.Value.Categories)
            {
                bool matches = pair.Key.EndsWith('*') ? name.StartsWith(pair.Key[..^1], StringComparison.OrdinalIgnoreCase) : name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase);
                if (matches)
                {
                    return pair.Value;
                }
            }
            return ServiceCategory.App;
        }
    }
}