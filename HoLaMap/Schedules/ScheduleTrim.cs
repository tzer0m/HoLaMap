namespace HoLaMap.Schedules
{
    /// <summary>
    /// Trims the empty start and end off a bar. The bar is split into equal cells, such as hours or days, and it's cut down to run from the first cell with something in it to the last.
    /// </summary>
    /// <param name="busy">Whether anything happens in each cell.</param>
    public class ScheduleTrim(bool[] busy)
    {
        /// <summary>
        /// Gets the first cell kept.
        /// </summary>
        public int First { get; } = Math.Max(Array.IndexOf(busy, true), 0);

        /// <summary>
        /// Gets the cell after the last one kept.
        /// </summary>
        public int Next { get; } = Array.LastIndexOf(busy, true) < 0 ? busy.Length : Array.LastIndexOf(busy, true) + 1;

        /// <summary>
        /// Gets where the trimmed bar starts, as a share of the whole bar.
        /// </summary>
        public double Start => (double)First / busy.Length;

        /// <summary>
        /// Gets where the trimmed bar ends, as a share of the whole bar.
        /// </summary>
        public double End => (double)Next / busy.Length;

        /// <summary>
        /// Moves a point on the whole bar to where it lands on the trimmed one.
        /// </summary>
        /// <param name="position">How far along the whole bar it is, from 0 to 1.</param>
        /// <returns>How far along the trimmed bar it is, from 0 to 1.</returns>
        public double Map(double position) => (position - Start) / (End - Start);

        /// <summary>
        /// Checks whether a point falls in the part trimmed off.
        /// </summary>
        /// <param name="position">How far along the whole bar it is, from 0 to 1.</param>
        /// <returns>Whether it's trimmed off.</returns>
        public bool Cuts(double position) => position < Start - 1e-9 || position > End + 1e-9;
    }
}