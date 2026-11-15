using System.ComponentModel.DataAnnotations;
using System.Reflection;
using HoLaMap.Models;

namespace HoLaMap.Schedules
{
    /// <summary>
    /// The names sources of scheduled tasks are shown by.
    /// </summary>
    public static class SourceLabels
    {
        /// <summary>
        /// Gets a source's name as shown on the pages.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>The name, like Systemd Timer.</returns>
        public static string Of(ScheduleSource source) => typeof(ScheduleSource).GetField(source.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? source.ToString();
    }
}