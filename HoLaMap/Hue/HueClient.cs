using HoLaMap.Hue.Models;
using Microsoft.Extensions.Options;

namespace HoLaMap.Hue
{
    /// <summary>
    /// Reads devices from a Hue Bridge over its local CLIP v2 API.
    /// </summary>
    /// <param name="http">The HTTP client, which accepts the bridge's self-signed certificate.</param>
    /// <param name="options">The Hue settings.</param>
    public class HueClient(HttpClient http, IOptions<HueOptions> options)
    {
        /// <summary>
        /// Gets every device on the bridge, including the bridge itself.
        /// </summary>
        /// <param name="bridgeIp">The bridge's IP address.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The devices.</returns>
        public Task<List<HueDevice>> GetDevicesAsync(string bridgeIp, CancellationToken cancellationToken) => GetAsync<HueDevice>(bridgeIp, "device", cancellationToken);

        /// <summary>
        /// Gets the Zigbee radio of every device, with whether the bridge can reach it.
        /// </summary>
        /// <param name="bridgeIp">The bridge's IP address.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The radios.</returns>
        public Task<List<HueZigbeeConnectivity>> GetZigbeeConnectivityAsync(string bridgeIp, CancellationToken cancellationToken) => GetAsync<HueZigbeeConnectivity>(bridgeIp, "zigbee_connectivity", cancellationToken);

        /// <summary>
        /// Gets every resource of one type.
        /// </summary>
        /// <typeparam name="T">The resource type.</typeparam>
        /// <param name="bridgeIp">The bridge's IP address.</param>
        /// <param name="resource">The resource type's name in the API.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The resources.</returns>
        private async Task<List<T>> GetAsync<T>(string bridgeIp, string resource, CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"https://{bridgeIp}/clip/v2/resource/{resource}");
            request.Headers.Add("hue-application-key", options.Value.AppKey);
            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            HueResponse<T>? body = await response.Content.ReadFromJsonAsync<HueResponse<T>>(cancellationToken);
            return body?.Data ?? [];
        }
    }
}