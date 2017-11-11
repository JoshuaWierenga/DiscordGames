// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GameStatus.cs" company="n\a">
//   n\a
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MafiaDiscord
{
    /// <summary>
    /// Indicates the status of game
    /// </summary>
    internal enum GameStatus
    {
        /// <summary>
        /// Indicates that the game has stopped
        /// </summary>
        Stopped,

        /// <summary>
        /// Indicates that the game is starting
        /// </summary>
        Starting,

        /// <summary>
        /// Indicates that the game is in a day phase
        /// </summary>
        Day,

        /// <summary>
        /// Indicates that the game is in a night phase and it is currently the mafia's turn
        /// </summary>
        NightMafia,

        /// <summary>
        /// Indicates that the game is in a night phase and it is currently the police's turn
        /// </summary>
        NightPolice,

        /// <summary>
        /// Indicates that the game is in a night phase and it is currently the doctor's turn
        /// </summary>
        NightDoctor
    }
}