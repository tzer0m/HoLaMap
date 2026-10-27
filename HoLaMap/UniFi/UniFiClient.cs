using System.Text.Json;
using System.Text.Json.Serialization;
using HoLaMap.UniFi.Models;
using Microsoft.Extensions.Options;

namespace HoLaMap.UniFi
{
    /// <summary>
    /// Reads data from the classic UniFi Network API using an API key.
    /// </summary>
    /// <param name="http">The HTTP client.</param>
    /// <param name="options">The UniFi settings.</param>
    public class UniFiClient(HttpClient http, IOptions<UniFiOptions> options)
    {
        /// <summary>
        /// JSON options tolerant of numbers sent as strings.
        /// </summary>
        private readonly JsonSerializerOptions JsonOptions = new() { NumberHandling = JsonNumberHandling.AllowReadingFromString };

        /// <summary>
        /// Gets all UniFi devices with their port tables.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The devices.</returns>
        public Task<List<UniFiDevice>> GetDevicesAsync(CancellationToken cancellationToken) => GetAsync<UniFiDevice>("stat/device", cancellationToken);

        /// <summary>
        /// Gets all currently connected clients.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The clients.</returns>
        public Task<List<UniFiStation>> GetActiveClientsAsync(CancellationToken cancellationToken) => GetAsync<UniFiStation>("stat/sta", cancellationToken);

        /// <summary>
        /// Gets all networks.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The networks.</returns>
        public Task<List<UniFiNetwork>> GetNetworksAsync(CancellationToken cancellationToken) => GetAsync<UniFiNetwork>("rest/networkconf", cancellationToken);

        /// <summary>
        /// Gets the data array from a classic API endpoint.
        /// </summary>
        /// <typeparam name="T">The record type.</typeparam>
        /// <param name="path">Path relative to the site.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The records.</returns>
        private async Task<List<T>> GetAsync<T>(string path, CancellationToken cancellationToken)
        {
            UniFiOptions settings = options.Value;
            using HttpRequestMessage request = new(HttpMethod.Get, new Uri(new Uri(settings.BaseUrl), $"proxy/network/api/s/{settings.Site}/{path}"));
            request.Headers.Add("X-API-KEY", settings.ApiKey);
            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            UniFiResponse<T>? body = await response.Content.ReadFromJsonAsync<UniFiResponse<T>>(JsonOptions, cancellationToken);
            return body?.Data ?? [];
        }
    }
}