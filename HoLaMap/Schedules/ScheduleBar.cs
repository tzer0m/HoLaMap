namespace HoLaMap.Schedules
{
    /// <summary>
    /// One of the three bars on the Schedule page, with the runs that fall on it.
    /// </summary>
    public class ScheduleBar
    {
        /// <summary>
        /// Gets or sets which bar it is.
        /// </summary>
        public SchedulePeriod Period { get; set; }

        /// <summary>
        /// Gets or sets the heading.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dates it covers in words.
        /// </summary>
        public string Span { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the marks along it.
        /// </summary>
        public List<ScheduleTick> Ticks { get; set; } = [];

        /// <summary>
        /// Gets or sets the runs drawn as blocks.
        /// </summary>
        public List<ScheduleBlock> Blocks { get; set; } = [];

        /// <summary>
        /// Gets or sets the tasks drawn as strips.
        /// </summary>
        public List<ScheduleStrip> Strips { get; set; } = [];

        /// <summary>
        /// Gets or sets how many rows the blocks are stacked in.
        /// </summary>
        public int Lanes { get; set; }

        /// <summary>
        /// Gets or sets how far along it now is, from 0 to 1.
        /// </summary>
        public double Now { get; set; }
    }
}