using System.Net.Http.Headers;
using System.Text.Json;
using HoLaMap.Data;
using HoLaMap.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HoLaMap.Cloudflare
{
    /// <summary>
    /// Reads the account's tunnels and their routes from Cloudflare's API. Routes picked by hand keep their service while their hostname stays on the tunnel.
    /// </summary>
    /// <param name="http">The HTTP client.</param>
    /// <param name="context">The database context.</param>
    /// <param name="options">The Cloudflare settings.</param>
    public class CloudflareSyncService(HttpClient http, HoLaMapContext context, IOptions<CloudflareOptions> options)
    {
        /// <summary>
        /// Updates the tunnels and routes to match Cloudflare. A tunnel no longer there is removed with its routes.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of tunnels.</returns>
        public async Task<int> SyncAsync(CancellationToken cancellationToken)
        {
            string account = options.Value.AccountId;
            using JsonDocument list = await GetAsync($"accounts/{account}/cfd_tunnel?is_deleted=false", cancellationToken);
            List<Tunnel> tunnels = await context.Tunnels.Include(tunnel => tunnel.Routes).ToListAsync(cancellationToken);
            HashSet<string> seen = [];
            DateTime now = DateTime.UtcNow;
            foreach (JsonElement item in list.RootElement.GetProperty("result").EnumerateArray())
            {
                string id = item.GetProperty("id").GetString() ?? string.Empty;
                seen.Add(id);
                Tunnel? tunnel = tunnels.FirstOrDefault(existing => existing.CloudflareId == id);
                if (tunnel == null)
                {
                    tunnel = new Tunnel { CloudflareId = id };
                    context.Tunnels.Add(tunnel);
                }
                tunnel.Name = Text(item, "name");
                tunnel.Status = Text(item, "status");
                tunnel.RemoteConfig = item.TryGetProperty("remote_config", out JsonElement remote) && remote.ValueKind == JsonValueKind.True;
                tunnel.Version = item.TryGetProperty("connections", out JsonElement connections) && connections.ValueKind == JsonValueKind.Array ? connections.EnumerateArray().Select(connection => Text(connection, "client_version")).FirstOrDefault(version => version.Length > 0) : null;
                tunnel.LastSeen = now;
                using JsonDocument configuration = await GetAsync($"accounts/{account}/cfd_tunnel/{id}/configurations", cancellationToken);
                SyncRoutes(tunnel, Ingress(configuration));
            }
            context.Tunnels.RemoveRange(tunnels.Where(tunnel => !seen.Contains(tunnel.CloudflareId)));
            await context.SaveChangesAsync(cancellationToken);
            return seen.Count;
        }

        /// <summary>
        /// Updates a tunnel's routes, keyed by hostname and path, keeping any service picked by hand.
        /// </summary>
        /// <param name="tunnel">The tunnel.</param>
        /// <param name="ingress">Its routes as Cloudflare has them.</param>
        private void SyncRoutes(Tunnel tunnel, List<TunnelRoute> ingress)
        {
            context.TunnelRoutes.RemoveRange(tunnel.Routes.Where(route => !ingress.Any(item => item.Hostname == route.Hostname && item.Path == route.Path)));
            foreach (TunnelRoute item in ingress)
            {
                TunnelRoute? route = tunnel.Routes.FirstOrDefault(existing => existing.Hostname == item.Hostname && existing.Path == item.Path);
                if (route == null)
                {
                    tunnel.Routes.Add(item);
                }
                else
                {
                    route.Target = item.Target;
                }
            }
        }

        /// <summary>
        /// Reads the routes from a tunnel's configuration, leaving out the catch-all rule at the end, which has no hostname.
        /// </summary>
        /// <param name="configuration">The configuration response.</param>
        /// <returns>The routes, not yet saved.</returns>
        private static List<TunnelRoute> Ingress(JsonDocument configuration)
        {
            if (!configuration.RootElement.TryGetProperty("result", out JsonElement result) || result.ValueKind != JsonValueKind.Object || !result.TryGetProperty("config", out JsonElement config) || config.ValueKind != JsonValueKind.Object || !config.TryGetProperty("ingress", out JsonElement ingress) || ingress.ValueKind != JsonValueKind.Array)
            {
                return [];
            }
            return [.. ingress.EnumerateArray().Where(rule => Text(rule, "hostname").Length > 0).Select(rule => new TunnelRoute { Hostname = Text(rule, "hostname").ToLowerInvariant(), Path = Text(rule, "path"), Target = Text(rule, "service") }).DistinctBy(route => (route.Hostname, route.Path))];
        }

        /// <summary>
        /// Calls Cloudflare's API with the token.
        /// </summary>
        /// <param name="path">The path after /client/v4/.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response.</returns>
        private async Task<JsonDocument> GetAsync(string path, CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/{path}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiToken);
            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }

        /// <summary>
        /// Gets a string property, or an empty string if it's missing or not a string.
        /// </summary>
        /// <param name="element">The JSON object.</param>
        /// <param name="name">The property name.</param>
        /// <returns>The value.</returns>
        private static string Text(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
        }
    }
}