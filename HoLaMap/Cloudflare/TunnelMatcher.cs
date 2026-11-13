using HoLaMap.Models;

namespace HoLaMap.Cloudflare
{
    /// <summary>
    /// Works out where tunnels run and which service each route reaches.
    /// </summary>
    public static class TunnelMatcher
    {
        /// <summary>
        /// Gets the device a tunnel's connector runs on: the one picked by hand, or else the device with the tunnel's name.
        /// </summary>
        /// <param name="tunnel">The tunnel.</param>
        /// <param name="devices">The devices, with their services.</param>
        /// <returns>The device, or null if there isn't one.</returns>
        public static Device? HostOf(Tunnel tunnel, List<Device> devices)
        {
            return tunnel.DeviceId != null ? devices.FirstOrDefault(device => device.Id == tunnel.DeviceId) : devices.FirstOrDefault(device => device.Name.Equals(tunnel.Name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds the service a route reaches from its target's host and port. Localhost is the tunnel's own device, and any other host is the device with that IP or name.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="tunnelHost">The device the tunnel runs on.</param>
        /// <param name="devices">The devices, with their services.</param>
        /// <returns>The service listening on the port, or null if none is.</returns>
        public static Service? Match(TunnelRoute route, Device? tunnelHost, List<Device> devices)
        {
            if (!Uri.TryCreate(route.Target, UriKind.Absolute, out Uri? uri) || uri.Port <= 0)
            {
                return null;
            }
            Device? device = uri.IsLoopback || uri.Host == "0.0.0.0" ? tunnelHost : devices.FirstOrDefault(item => item.Ip == uri.Host || item.Name.Equals(uri.Host, StringComparison.OrdinalIgnoreCase));
            return device?.Services.FirstOrDefault(service => service.Ports.Contains(uri.Port));
        }
    }
}