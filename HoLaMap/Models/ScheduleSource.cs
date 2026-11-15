using System.ComponentModel.DataAnnotations;

namespace HoLaMap.Models
{
    /// <summary>
    /// Where a scheduled task was read from.
    /// </summary>
    public enum ScheduleSource
    {
        /// <summary>
        /// A Semaphore template's schedule.
        /// </summary>
        Semaphore,

        /// <summary>
        /// A systemd timer, reported by its host.
        /// </summary>
        [Display(Name = "Systemd Timer")]
        SystemdTimer
    }
}