namespace HoLaMap.Models
{
    /// <summary>
    /// Which side of a patch panel port a cable plugs into.
    /// </summary>
    public enum PatchSide
    {
        /// <summary>
        /// The front socket, usually patched to a switch in the rack.
        /// </summary>
        Front,

        /// <summary>
        /// The back socket, usually the run out to another room.
        /// </summary>
        Back
    }
}