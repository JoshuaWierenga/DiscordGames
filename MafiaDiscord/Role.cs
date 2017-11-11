// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Role.cs" company="n\a">
//   n\a
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MafiaDiscord
{
    /// <summary>
    /// Indicates the role of a mafia player
    /// </summary>
    internal enum Role
    {
        /// <summary>
        /// Indicates that the player is a member of the mafia 
        /// </summary>
        Mafia,

        /// <summary>
        /// Indicates that the player is a member of the police
        /// </summary>
        Police,

        /// <summary>
        /// Indicates that the player is a doctor
        /// </summary>
        Doctor,

        /// <summary>
        /// Indicates that the player is a civilian
        /// </summary>
        Civilian
    }
}