namespace HoLaMap.Power
{
    /// <summary>
    /// Whether a machine on the Power page is up.
    /// </summary>
    public class MachineStatus
    {
        /// <summary>
        /// Gets or sets the device ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets whether it answered.
        /// </summary>
        public bool Online { get; set; }
    }
}