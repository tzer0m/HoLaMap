using System.Diagnostics;
using HoLaMap.Models;
using Microsoft.Extensions.Options;

namespace HoLaMap.Power
{
    /// <summary>
    /// Wakes machines by sending a wake packet with etherwake.
    /// </summary>
    /// <param name="options">The power settings.</param>
    public class WakeOnLanService(IOptions<PowerOptions> options)
    {
        /// <summary>
        /// Sends a wake packet to a machine.
        /// </summary>
        /// <param name="device">The machine.</param>
        /// <returns>Whether the packet was sent, and the message to show.</returns>
        public async Task<PowerResult> WakeAsync(Device device)
        {
            if (string.IsNullOrWhiteSpace(device.WakeInterface))
            {
                return new PowerResult { Success = false, Message = $"{device.Name} has no wake interface set." };
            }
            PowerOptions settings = options.Value;
            ProcessStartInfo startInfo = new(settings.SudoPath) { RedirectStandardError = true };
            startInfo.ArgumentList.Add("-n");
            startInfo.ArgumentList.Add(settings.EtherWakePath);
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(device.WakeInterface);
            startInfo.ArgumentList.Add(device.WakeMac ?? device.Mac);
            try
            {
                using Process? process = Process.Start(startInfo);
                if (process == null)
                {
                    return new PowerResult { Success = false, Message = $"Couldn't start the wake command for {device.Name}." };
                }
                using CancellationTokenSource cancellation = new(settings.CommandTimeoutMs);
                string error = await process.StandardError.ReadToEndAsync(cancellation.Token);
                await process.WaitForExitAsync(cancellation.Token);
                return process.ExitCode == 0 ? new PowerResult { Success = true, Message = $"Wake packet sent to {device.Name}." } : new PowerResult { Success = false, Message = $"Wake failed for {device.Name}: {error.Trim()}" };
            }
            catch (Exception exception)
            {
                return new PowerResult { Success = false, Message = $"Wake failed for {device.Name}: {exception.Message}" };
            }
        }
    }
}