using System.Text.Json;
using System.Text.Json.Serialization;
using HoLaMap.UniFi.Models;
using Microsoft.Extensions.Options;

namespace HoLaMap.UniFi
{
    /// <summary>
    /// Reads data from the classic UniFi Network API using an API key.
    /// </summary>
    public class UniFiClient
    {
        /// <summary>
        /// The HTTP client.
        /// </summary>
        private readonly HttpClient Http;

        /// <summary>
        /// The UniFi settings.
        /// </summary>
        private readonly UniFiOptions Options;

        /// <summary>
        /// JSON options tolerant of numbers sent as strings.
        /// </summary>
        private readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString };

        /// <summary>
        /// Initializes a new instance of the <see cref="UniFiClient"/> class.
        /// </summary>
        /// <param name="http">The HTTP client.</param>
        /// <param name="options">The UniFi settings.</param>
        public UniFiClient(HttpClient http, IOptions<UniFiOptions> options)
        {
            Http = http;
            Options = options.Value;
            Http.BaseAddress = new Uri(Options.BaseUrl);
            Http.DefaultRequestHeaders.Add("X-API-KEY", Options.ApiKey);
        }

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
            UniFiResponse<T>? response = await Http.GetFromJsonAsync<UniFiResponse<T>>($"proxy/network/api/s/{Options.Site}/{path}", JsonOptions, cancellationToken);
            return response?.Data ?? new List<T>();
        }
    }
}