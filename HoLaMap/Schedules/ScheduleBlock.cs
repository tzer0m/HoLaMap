namespace HoLaMap.Schedules
{
    /// <summary>
    /// One run of a task on a bar, as wide as the run usually takes.
    /// </summary>
    public class ScheduleBlock
    {
        /// <summary>
        /// Gets or sets the identifier of the task.
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Gets or sets the task's name, shown on the block when it fits.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the text shown on hover.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the host it runs against, which sets its colour.
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets how far along the bar it starts, from 0 to 1.
        /// </summary>
        public double Start { get; set; }

        /// <summary>
        /// Gets or sets how much of the bar it covers, from 0 to 1.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Gets or sets the row it's stacked in, 0 being the top.
        /// </summary>
        public int Lane { get; set; }

        /// <summary>
        /// Gets or sets whether its schedule is turned off.
        /// </summary>
        public bool Inactive { get; set; }
    }
}