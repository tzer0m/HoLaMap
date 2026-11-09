using System.Text.Json;
using HoLaMap.Hosts.Models;
using HoLaMap.Models;
using Microsoft.Extensions.Options;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Reads each API's OpenAPI description and saves its first-level routes as services grouped under the API, so links can point at a route rather than the whole API.
    /// </summary>
    /// <param name="http">The HTTP client.</param>
    /// <param name="sync">The host sync service.</param>
    /// <param name="options">The OpenAPI settings.</param>
    /// <param name="logger">The logger.</param>
    public class OpenApiSyncService(HttpClient http, HostSyncService sync, IOptions<OpenApiOptions> options, ILogger<OpenApiSyncService> logger)
    {
        /// <summary>
        /// Syncs every API. One that can't be read is logged and skipped, leaving its routes as they were.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when every API has been tried.</returns>
        public async Task SyncAsync(CancellationToken cancellationToken)
        {
            foreach (OpenApiSource source in options.Value.Sources)
            {
                try
                {
                    using JsonDocument document = JsonDocument.Parse(await http.GetStringAsync(source.Url, cancellationToken));
                    int? count = await sync.SyncAsync(source.Host, ServiceKind.ApiRoute, Routes(document, source.Service), cancellationToken, source.Service);
                    if (count == null)
                    {
                        logger.LogWarning("No device called {Host}, so the {Service} routes weren't saved", source.Host, source.Service);
                    }
                }
                catch (Exception exception) when (exception is HttpRequestException or JsonException || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
                {
                    logger.LogError(exception, "Couldn't read the {Service} OpenAPI description from {Url}", source.Service, source.Url);
                }
            }
        }

        /// <summary>
        /// Gets an API's routes split by the first part of their paths, so /Chitter/Print and /Chitter/Quote are both Chitter.
        /// </summary>
        /// <param name="document">The OpenAPI description.</param>
        /// <param name="service">The API's service name, which the routes are grouped under.</param>
        /// <returns>The routes as services.</returns>
        public static List<ReportedService> Routes(JsonDocument document, string service)
        {
            if (!document.RootElement.TryGetProperty("paths", out JsonElement paths) || paths.ValueKind != JsonValueKind.Object)
            {
                return [];
            }
            return [.. paths.EnumerateObject().Select(path => path.Name.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(segment => segment).Select(segment => new ReportedService { Name = segment, Custom = true, Group = service })];
        }
    }
}