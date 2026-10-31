namespace HoLaMap.Models
{
    /// <summary>
    /// A physical or virtual device on the network, identified by its MAC address.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the kind of device.
        /// </summary>
        public DeviceKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the model, if known.
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the MAC address in lowercase colon format.
        /// </summary>
        public string Mac { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last known IP address.
        /// </summary>
        public string? Ip { get; set; }

        /// <summary>
        /// Gets or sets when the device was last seen (UTC).
        /// </summary>
        public DateTime LastSeen { get; set; }

        /// <summary>
        /// Gets or sets the device's ports.
        /// </summary>
        public List<Port> Ports { get; set; } = [];

        /// <summary>
        /// Gets or sets extra MAC addresses from devices merged into this one.
        /// </summary>
        public List<DeviceMac> Macs { get; set; } = [];

        /// <summary>
        /// Gets or sets the identifier of the room the device is in, if placed.
        /// </summary>
        public int? RoomId { get; set; }

        /// <summary>
        /// Gets or sets the room the device is in, if placed.
        /// </summary>
        public Room? Room { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the rack the device is in, if any.
        /// </summary>
        public int? RackId { get; set; }

        /// <summary>
        /// Gets or sets the rack the device is in, if any.
        /// </summary>
        public Rack? Rack { get; set; }

        /// <summary>
        /// Gets or sets the lowest rack unit the device occupies, counting from 1 at the bottom.
        /// </summary>
        public int? RackU { get; set; }

        /// <summary>
        /// Gets or sets the left column the device occupies, from 0 to 3.
        /// </summary>
        public int? RackColumn { get; set; }

        /// <summary>
        /// Gets or sets how many of the four columns the device is wide: 1, 2 or 4.
        /// </summary>
        public int RackWidth { get; set; } = 4;

        /// <summary>
        /// Gets or sets how many rack units tall the device is.
        /// </summary>
        public int RackHeight { get; set; } = 1;

        /// <summary>
        /// Gets or sets the identifier of the access point the device was last connected to over Wi-Fi, or null if it's wired.
        /// </summary>
        public int? WirelessApId { get; set; }

        /// <summary>
        /// Gets or sets the access point the device was last connected to over Wi-Fi, or null if it's wired.
        /// </summary>
        public Device? WirelessAp { get; set; }

        /// <summary>
        /// Gets or sets the Wi-Fi network the device was last on.
        /// </summary>
        public string? Ssid { get; set; }

        /// <summary>
        /// Gets or sets the Wi-Fi band the device was last on, such as 5 GHz.
        /// </summary>
        public string? WifiBand { get; set; }

        /// <summary>
        /// Gets or sets the Wi-Fi channel the device was last on.
        /// </summary>
        public int? WifiChannel { get; set; }

        /// <summary>
        /// Gets or sets the last Wi-Fi signal strength in dBm.
        /// </summary>
        public int? WifiSignal { get; set; }

        /// <summary>
        /// Gets or sets the last Wi-Fi link rate in Mbps.
        /// </summary>
        public int? WifiRate { get; set; }
    }
}