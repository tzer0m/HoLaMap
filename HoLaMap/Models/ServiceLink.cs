namespace HoLaMap.Models
{
    /// <summary>
    /// A dependency between two services, where the first uses the second.
    /// </summary>
    public class ServiceLink
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the service that uses the other.
        /// </summary>
        public int FromServiceId { get; set; }

        /// <summary>
        /// Gets or sets the service that uses the other.
        /// </summary>
        public Service FromService { get; set; } = null!;

        /// <summary>
        /// Gets or sets the identifier of the service being used.
        /// </summary>
        public int ToServiceId { get; set; }

        /// <summary>
        /// Gets or sets the service being used.
        /// </summary>
        public Service ToService { get; set; } = null!;

        /// <summary>
        /// Gets or sets what the service is used for.
        /// </summary>
        public ServiceLinkKind Kind { get; set; }

        /// <summary>
        /// Gets or sets optional notes about the link.
        /// </summary>
        public string? Notes { get; set; }
    }
}