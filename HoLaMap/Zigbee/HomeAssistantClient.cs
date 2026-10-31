using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using HoLaMap.Zigbee.Models;
using Microsoft.Extensions.Options;

namespace HoLaMap.Zigbee
{
    /// <summary>
    /// Reads Zigbee devices from Home Assistant's ZHA integration over the WebSocket API, since ZHA's device and neighbour lists aren't in the REST API.
    /// </summary>
    /// <param name="options">The Home Assistant settings.</param>
    public class HomeAssistantClient(IOptions<HomeAssistantOptions> options)
    {
        /// <summary>
        /// JSON options tolerant of numbers sent as strings, which ZHA does for link quality.
        /// </summary>
        private readonly JsonSerializerOptions JsonOptions = new() { NumberHandling = JsonNumberHandling.AllowReadingFromString };

        /// <summary>
        /// Gets every Zigbee device ZHA knows about, with its neighbour table.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The devices.</returns>
        public async Task<List<ZhaDevice>> GetZhaDevicesAsync(CancellationToken cancellationToken)
        {
            HomeAssistantOptions settings = options.Value;
            UriBuilder address = new(new Uri(new Uri(settings.Url), "api/websocket")) { Scheme = settings.Url.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws" };
            using ClientWebSocket socket = new();
            await socket.ConnectAsync(address.Uri, cancellationToken);
            await ReceiveAsync(socket, cancellationToken);
            await SendAsync(socket, new JsonObject { ["type"] = "auth", ["access_token"] = settings.Token }, cancellationToken);
            JsonNode? auth = await ReceiveAsync(socket, cancellationToken);
            if (auth?["type"]?.GetValue<string>() != "auth_ok")
            {
                throw new InvalidOperationException("Home Assistant refused the access token.");
            }
            await SendAsync(socket, new JsonObject { ["id"] = 1, ["type"] = "zha/devices" }, cancellationToken);
            JsonNode? reply = await ReceiveAsync(socket, cancellationToken);
            if (reply?["success"]?.GetValue<bool>() != true)
            {
                throw new InvalidOperationException($"Home Assistant couldn't list ZHA devices: {reply?["error"]?["message"]}");
            }
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
            return reply["result"]?.Deserialize<List<ZhaDevice>>(JsonOptions) ?? [];
        }

        /// <summary>
        /// Sends one message.
        /// </summary>
        /// <param name="socket">The open socket.</param>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the message has been sent.</returns>
        private static async Task SendAsync(ClientWebSocket socket, JsonObject message, CancellationToken cancellationToken)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(message.ToJsonString()), WebSocketMessageType.Text, true, cancellationToken);
        }

        /// <summary>
        /// Receives one whole message, which can arrive in several frames.
        /// </summary>
        /// <param name="socket">The open socket.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The parsed message.</returns>
        private static async Task<JsonNode?> ReceiveAsync(ClientWebSocket socket, CancellationToken cancellationToken)
        {
            using MemoryStream message = new();
            byte[] buffer = new byte[16384];
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken);
                message.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);
            return JsonNode.Parse(message.ToArray());
        }
    }
}