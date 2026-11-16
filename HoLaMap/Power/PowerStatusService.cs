using System.Net.NetworkInformation;
using System.Net.Sockets;
using HoLaMap.Models;
using Microsoft.Extensions.Options;

namespace HoLaMap.Power
{
    /// <summary>
    /// Checks whether machines on the Power page are up.
    /// </summary>
    /// <param name="options">The power settings.</param>
    public class PowerStatusService(IOptions<PowerOptions> options)
    {
        /// <summary>
        /// Checks every machine at once.
        /// </summary>
        /// <param name="devices">The machines.</param>
        /// <returns>Whether each machine is up.</returns>
        public async Task<List<MachineStatus>> CheckAllAsync(List<Device> devices)
        {
            MachineStatus[] statuses = await Task.WhenAll(devices.Select(CheckAsync));
            return [.. statuses];
        }

        /// <summary>
        /// Checks one machine, on its check port if it has one, otherwise by pinging it.
        /// </summary>
        /// <param name="device">The machine.</param>
        /// <returns>Whether it is up.</returns>
        public async Task<MachineStatus> CheckAsync(Device device)
        {
            bool online = !string.IsNullOrWhiteSpace(device.Ip) && (device.PowerCheckPort.HasValue ? await CheckPortAsync(device.Ip, device.PowerCheckPort.Value) : await PingAsync(device.Ip));
            return new MachineStatus { Id = device.Id, Online = online };
        }

        /// <summary>
        /// Checks whether a TCP port accepts connections.
        /// </summary>
        /// <param name="host">The IP address.</param>
        /// <param name="port">The port.</param>
        /// <returns>Whether it connected in time.</returns>
        private async Task<bool> CheckPortAsync(string host, int port)
        {
            try
            {
                using TcpClient client = new();
                using CancellationTokenSource cancellation = new(options.Value.CheckTimeoutMs);
                await client.ConnectAsync(host, port, cancellation.Token);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether a host answers a ping.
        /// </summary>
        /// <param name="host">The IP address.</param>
        /// <returns>Whether it answered in time.</returns>
        private async Task<bool> PingAsync(string host)
        {
            try
            {
                using Ping ping = new();
                PingReply reply = await ping.SendPingAsync(host, options.Value.CheckTimeoutMs);
                return reply.Status == IPStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}