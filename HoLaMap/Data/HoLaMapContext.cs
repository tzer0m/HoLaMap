using HoLaMap.Models;
using Microsoft.EntityFrameworkCore;

namespace HoLaMap.Data
{
    /// <summary>
    /// Database context for HoLaMap.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="HoLaMapContext"/> class.
    /// </remarks>
    /// <param name="options">Context options.</param>
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
            modelBuilder.Entity<Link>().HasIndex(link => link.FromPortId).IsUnique();
            modelBuilder.Entity<Network>().HasIndex(network => network.Name).IsUnique();
        }
    }
}