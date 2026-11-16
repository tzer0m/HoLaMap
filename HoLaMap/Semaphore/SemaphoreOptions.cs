namespace HoLaMap.Semaphore
{
    /// <summary>
    /// Settings for reading schedules from Semaphore.
    /// </summary>
    public class SemaphoreOptions
    {
        /// <summary>
        /// Gets or sets Semaphore's address, such as http://localhost:3000.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an API token for a user who can see the project.
        /// </summary>
        public string ApiToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the project to read.
        /// </summary>
        public int ProjectId { get; set; } = 1;

        /// <summary>
        /// Gets or sets the time zone Semaphore runs its schedules in.
        /// </summary>
        public string TimeZone { get; set; } = "UTC";

        /// <summary>
        /// Gets or sets how many recent runs to keep durations for.
        /// </summary>
        public int DurationCount { get; set; } = 10;

        /// <summary>
        /// Gets or sets the number of minutes between syncs.
        /// </summary>
        public int SyncIntervalMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets the ID of the template that powers machines off or puts them to sleep.
        /// </summary>
        public int PowerTemplateId { get; set; }
    }
}