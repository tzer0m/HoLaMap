namespace HoLaMap.Schedules
{
    /// <summary>
    /// How often a schedule comes round, which decides the bar it's drawn on.
    /// </summary>
    public enum SchedulePeriod
    {
        /// <summary>
        /// Every hour or more often, drawn as one hour.
        /// </summary>
        Hour,

        /// <summary>
        /// Every day, drawn noon to noon.
        /// </summary>
        Day,

        /// <summary>
        /// On certain days of the week, drawn Monday to Sunday.
        /// </summary>
        Week,

        /// <summary>
        /// On certain days of the month, drawn 1 to 31.
        /// </summary>
        Month
    }
}