namespace HoLaMap.Hosts.Models
{
    /// <summary>
    /// A TCP port a process on a host is listening on.
    /// </summary>
    public class ListeningPort
    {
        /// <summary>
        /// Gets or sets the process name, which Linux cuts to 15 characters.
        /// </summary>
        public string Process { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        public int Port { get; set; }
    }
}