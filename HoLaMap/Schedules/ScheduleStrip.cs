namespace HoLaMap.Schedules
{
    /// <summary>
    /// A task that runs too often to draw each run as a block, drawn as a thin row with a mark for each run.
    /// </summary>
    public class ScheduleStrip
    {
        /// <summary>
        /// Gets or sets the identifier of the task.
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Gets or sets the label shown on the row.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the host it runs against, which sets its colour.
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets how far along the bar each run is, from 0 to 1.
        /// </summary>
        public List<double> Positions { get; set; } = [];

        /// <summary>
        /// Gets or sets whether its schedule is turned off.
        /// </summary>
        public bool Inactive { get; set; }
    }
}