using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HoLaMap.Models;
using HoLaMap.Semaphore;
using Microsoft.Extensions.Options;

namespace HoLaMap.Power
{
    /// <summary>
    /// Powers machines off or puts them to sleep by running Semaphore's power template.
    /// </summary>
    /// <param name="http">The HTTP client.</param>
    /// <param name="options">The Semaphore settings.</param>
    public class PowerActionService(HttpClient http, IOptions<SemaphoreOptions> options)
    {
        /// <summary>
        /// Starts the power template for a machine.
        /// </summary>
        /// <param name="device">The machine.</param>
        /// <param name="action">Whether to power it off or put it to sleep.</param>
        /// <returns>Whether the task started, and the message to show.</returns>
        public async Task<PowerResult> RunAsync(Device device, PowerAction action)
        {
            string label = action == PowerAction.Shutdown ? "Power off" : "Sleep";
            if (string.IsNullOrWhiteSpace(device.SemaphoreHost))
            {
                return new PowerResult { Success = false, Message = $"{device.Name} has no Semaphore host set." };
            }
            if (action == PowerAction.Sleep && !device.CanSleep)
            {
                return new PowerResult { Success = false, Message = $"{device.Name} can't be put to sleep." };
            }
            SemaphoreOptions settings = options.Value;
            if (settings.BaseUrl.Length == 0 || settings.PowerTemplateId == 0)
            {
                return new PowerResult { Success = false, Message = "Semaphore's power template isn't set up." };
            }
            Dictionary<string, object> body = new() { ["template_id"] = settings.PowerTemplateId, ["limit"] = device.SemaphoreHost, ["environment"] = JsonSerializer.Serialize(new { power_action = action == PowerAction.Shutdown ? "shutdown" : "sleep" }) };
            if (device.SemaphoreInventoryId != null)
            {
                body["inventory_id"] = device.SemaphoreInventoryId.Value;
            }
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/api/project/{settings.ProjectId}/tasks") { Content = JsonContent.Create(body) };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                using HttpResponseMessage response = await http.SendAsync(request);
                return response.IsSuccessStatusCode ? new PowerResult { Success = true, Message = $"{label} started for {device.Name}." } : new PowerResult { Success = false, Message = $"Semaphore returned {(int)response.StatusCode} for {device.Name}." };
            }
            catch (Exception exception)
            {
                return new PowerResult { Success = false, Message = $"Couldn't reach Semaphore for {device.Name}: {exception.Message}" };
            }
        }
    }
}