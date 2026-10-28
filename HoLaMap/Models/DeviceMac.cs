namespace HoLaMap.Models
{
    /// <summary>
    /// An extra MAC address belonging to a device, kept when devices are merged.
    /// </summary>
    public class DeviceMac
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the owning device identifier.
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the owning device.
        /// </summary>
        public Device Device { get; set; } = null!;

        /// <summary>
        /// Gets or sets the MAC address in lowercase colon format.
        /// </summary>
        public string Mac { get; set; } = string.Empty;
    }
}