using HoLaMap.Data;
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
        /// <param name="names">The names of the services running.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of services reported, or null if no device matches the host.</returns>
        public async Task<int?> SyncAsync(string host, ServiceKind kind, List<string> names, CancellationToken cancellationToken)
        {
            string name = host.Trim().ToLowerInvariant();
            string mac = name.Replace('-', ':');
            Device? device = await context.Devices.Include(item => item.Services).FirstOrDefaultAsync(item => item.Name.ToLower() == name || item.Mac == mac, cancellationToken);
            if (device == null)
            {
                return null;
            }
            DateTime now = DateTime.UtcNow;
            HashSet<string> reported = [.. names];

            // A service that has gone from its host stays greyed out in case it comes back, and is forgotten once it hasn't been reported for a while.
            int forgetAfterDays = options.Value.ForgetAfterDays;
            foreach (Service missing in device.Services.Where(service => service.Kind == kind && service.Source == ServiceSource.Synced && !reported.Contains(service.Name)).ToList())
            {
                if (forgetAfterDays > 0 && missing.LastSeen < now.AddDays(-forgetAfterDays))
                {
                    context.Services.Remove(missing);
                }
            }
            foreach (string serviceName in reported)
            {
                Service? service = device.Services.FirstOrDefault(item => item.Kind == kind && item.Name == serviceName);
                if (service == null)
                {
                    service = new Service { Name = serviceName, Kind = kind, Source = ServiceSource.Synced };
                    device.Services.Add(service);
                }
                if (service.Source == ServiceSource.Synced)
                {
                    service.LastSeen = now;
                    service.Category = CategoryOf(serviceName);
                }
            }
            await context.SaveChangesAsync(cancellationToken);
            return reported.Count;
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