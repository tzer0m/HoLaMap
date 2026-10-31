using HoLaMap.Models;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Data
{
    /// <summary>
    /// Database context for HoLaMap.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public class HoLaMapContext(DbContextOptions<HoLaMapContext> options) : DbContext(options)
    {
        /// <summary>
        /// Gets the devices.
        /// </summary>
        public DbSet<Device> Devices => Set<Device>();

        /// <summary>
        /// Gets the ports.
        /// </summary>
        public DbSet<Port> Ports => Set<Port>();

        /// <summary>
        /// Gets the links.
        /// </summary>
        public DbSet<Link> Links => Set<Link>();

        /// <summary>
        /// Gets the networks.
        /// </summary>
        public DbSet<Network> Networks => Set<Network>();

        /// <summary>
        /// Gets the action items.
        /// </summary>
        public DbSet<ActionItem> ActionItems => Set<ActionItem>();

        /// <summary>
        /// Gets the extra MAC addresses of merged devices.
        /// </summary>
        public DbSet<DeviceMac> DeviceMacs => Set<DeviceMac>();

        /// <summary>
        /// Gets the floors.
        /// </summary>
        public DbSet<Floor> Floors => Set<Floor>();

        /// <summary>
        /// Gets the rooms.
        /// </summary>
        public DbSet<Room> Rooms => Set<Room>();

        /// <summary>
        /// Gets the racks.
        /// </summary>
        public DbSet<Rack> Racks => Set<Rack>();

        /// <summary>
        /// Gets the blank and brush panels in racks.
        /// </summary>
        public DbSet<RackPanel> RackPanels => Set<RackPanel>();

        /// <summary>
        /// Gets the cables plugged into patch panel ports.
        /// </summary>
        public DbSet<PatchConnection> PatchConnections => Set<PatchConnection>();

        /// <summary>
        /// Configures keys, indexes and relationships.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Device>().HasIndex(device => device.Mac).IsUnique();
            modelBuilder.Entity<Device>().Property(device => device.Kind).HasConversion<string>();
            modelBuilder.Entity<Port>().HasIndex(port => new { port.DeviceId, port.Index }).IsUnique();
            modelBuilder.Entity<Link>().HasOne(link => link.FromPort).WithMany().HasForeignKey(link => link.FromPortId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Link>().HasOne(link => link.ToDevice).WithMany().HasForeignKey(link => link.ToDeviceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Link>().HasOne(link => link.ToPort).WithMany().HasForeignKey(link => link.ToPortId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Link>().HasIndex(link => link.FromPortId);
            modelBuilder.Entity<Link>().Property(link => link.Source).HasConversion<string>();
            modelBuilder.Entity<Network>().HasIndex(network => network.Name).IsUnique();
            modelBuilder.Entity<ActionItem>().HasIndex(item => new { item.PortId, item.DeviceId }).IsUnique();
            modelBuilder.Entity<DeviceMac>().HasIndex(deviceMac => deviceMac.Mac).IsUnique();
            modelBuilder.Entity<Floor>().HasIndex(floor => floor.Name).IsUnique();
            modelBuilder.Entity<Room>().HasIndex(room => new { room.FloorId, room.Name }).IsUnique();
            modelBuilder.Entity<Room>().HasOne(room => room.Floor).WithMany(floor => floor.Rooms).HasForeignKey(room => room.FloorId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Rack>().HasIndex(rack => new { rack.RoomId, rack.Name }).IsUnique();
            modelBuilder.Entity<Rack>().HasOne(rack => rack.Room).WithMany(room => room.Racks).HasForeignKey(rack => rack.RoomId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Device>().HasOne(device => device.WirelessAp).WithMany().HasForeignKey(device => device.WirelessApId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Device>().HasOne(device => device.Rack).WithMany(rack => rack.Devices).HasForeignKey(device => device.RackId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<RackPanel>().HasOne(panel => panel.Rack).WithMany(rack => rack.Panels).HasForeignKey(panel => panel.RackId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RackPanel>().Property(panel => panel.Kind).HasConversion<string>();
            modelBuilder.Entity<PatchConnection>().HasOne(connection => connection.RackPanel).WithMany(panel => panel.Connections).HasForeignKey(connection => connection.RackPanelId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PatchConnection>().HasOne(connection => connection.Device).WithMany().HasForeignKey(connection => connection.DeviceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PatchConnection>().HasOne(connection => connection.Port).WithMany().HasForeignKey(connection => connection.PortId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<PatchConnection>().HasIndex(connection => new { connection.RackPanelId, connection.Number, connection.Side }).IsUnique();
            modelBuilder.Entity<PatchConnection>().Property(connection => connection.Side).HasConversion<string>();
            modelBuilder.Entity<PatchConnection>().HasOne(connection => connection.TargetPanel).WithMany().HasForeignKey(connection => connection.TargetPanelId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PatchConnection>().Property(connection => connection.TargetSide).HasConversion<string>();
            modelBuilder.Entity<Device>().HasOne(device => device.Room).WithMany(room => room.Devices).HasForeignKey(device => device.RoomId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}