// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Player.cs" company="n\a">
//   n\a
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MafiaDiscord
{
    using DSharpPlus.Entities;

    /// <summary>
    /// Object for mafia players
    /// </summary>
    internal class Player
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Player"/> class.
        /// </summary>
        /// <param name="discordUser">
        /// Discord user that this player belongs too
        /// </param>
        public Player(DiscordUser discordUser)
        {
            this.DiscordUser = discordUser;
            this.Role = Role.Civilian;
            this.Votes = 0;
            this.Voted = false;
            this.Alive = true;
        }

        /// <summary>
        /// Gets or sets the discord user that this player belongs too
        /// </summary>
        public DiscordUser DiscordUser { get; set; }

        /// <summary>
        /// Gets or sets the user's current role
        /// </summary>
        public Role Role { get; set; }

        /// <summary>
        /// Gets or sets the number of votes against the player
        /// </summary>
        public int Votes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the player has voted 
        /// </summary>
        public bool Voted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the player is alive
        /// </summary>
        public bool Alive { get; set; }
    }
}