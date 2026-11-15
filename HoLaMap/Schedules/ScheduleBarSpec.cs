namespace HoLaMap.Schedules
{
    /// <summary>
    /// What a bar covers and how it's split up, before any runs are placed on it.
    /// </summary>
    /// <param name="Period">Which bar it is.</param>
    /// <param name="Title">The heading.</param>
    /// <param name="Span">The dates it covers in words.</param>
    /// <param name="Start">Where it starts, in the bars' time zone.</param>
    /// <param name="Length">How long it covers.</param>
    /// <param name="Cells">How many equal cells it's split into for trimming, such as 24 hours.</param>
    /// <param name="Edge">Labels the edge before a cell, used for the ends once trimmed, or null if the bar labels its cells instead.</param>
    /// <param name="Covers">How much of the bar runs are taken from, when it's less than its length, like a 30-day month on the 31-day month bar; null for all of it.</param>
    public record ScheduleBarSpec(SchedulePeriod Period, string Title, string Span, DateTime Start, TimeSpan Length, int Cells, Func<int, string>? Edge, TimeSpan? Covers = null);
}