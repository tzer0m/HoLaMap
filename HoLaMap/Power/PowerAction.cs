namespace HoLaMap.Power
{
    /// <summary>
    /// What Semaphore's power template does to a machine.
    /// </summary>
    public enum PowerAction
    {
        /// <summary>
        /// Powers it off.
        /// </summary>
        Shutdown,

        /// <summary>
        /// Puts it to sleep.
        /// </summary>
        Sleep
    }
}