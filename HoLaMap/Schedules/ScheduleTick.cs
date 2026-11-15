namespace HoLaMap.Schedules
{
    /// <summary>
    /// A mark along a bar, such as an hour or a day.
    /// </summary>
    public class ScheduleTick
    {
        /// <summary>
        /// Gets or sets how far along the bar it is, from 0 to 1.
        /// </summary>
        public double Position { get; set; }

        /// <summary>
        /// Gets or sets its label, or an empty string for a line on its own.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether a line is drawn down the bar here.
        /// </summary>
        public bool Line { get; set; }
    }
}