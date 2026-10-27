using HoLaMap.Models;

namespace HoLaMap.Display
{
    /// <summary>
    /// Works out how a port is shown: its colour class and tooltip.
    /// </summary>
    public static class PortStyle
    {
        /// <summary>
        /// Gets the CSS class for a port based on its state, speed and PoE draw.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns>The CSS class.</returns>
        public static string Class(Port port)
        {
            if (!port.Up)
            {
                return "port-down";
            }
            if (IsWan(port))
            {
                return "port-wan";
            }
            if (port.Speed < 1000)
            {
                return "port-fe";
            }
            return port.PoEPower > 0 ? "port-gbe-poe" : "port-gbe";
        }

        /// <summary>
        /// Gets the tooltip text for a port.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns>The port name, speed, network and PoE draw, skipping anything unknown.</returns>
        public static string Tooltip(Port port)
        {
            List<string?> parts = [port.Name, port.Speed != null ? $"{port.Speed} Mbps" : "Down", port.NetworkName, port.PoEPower > 0 ? $"{port.PoEPower:0.0} W" : null];
            return string.Join(" · ", parts.Where(part => !string.IsNullOrEmpty(part)));
        }

        /// <summary>
        /// Gets whether a port carries the internet connection.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns>True if the port is a WAN port.</returns>
        private static bool IsWan(Port port)
        {
            return (port.Name?.Contains("WAN", StringComparison.OrdinalIgnoreCase) ?? false) || (port.NetworkName?.StartsWith("WAN", StringComparison.OrdinalIgnoreCase) ?? false) || (port.NetworkName?.StartsWith("Internet", StringComparison.OrdinalIgnoreCase) ?? false);
        }
    }
}