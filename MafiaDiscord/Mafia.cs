// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Mafia.cs" company="n/a">
//   n/a
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MafiaDiscord
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    /// <summary>
    /// Handles playing mafia
    /// </summary>
    internal class Mafia
    {
        /// <summary>
        /// The number of mafia in the game
        /// </summary>
        private readonly int mafiaCount;

        /// <summary>
        /// The number of police in the game
        /// </summary>
        private readonly int policeCount;

        /// <summary>
        /// Whether or not this game has a doctor
        /// </summary>
        private readonly bool haveDoctor;

        /// <summary>
        /// Instance of a Discord Client
        /// </summary>
        private readonly DiscordClient client;

        /// <summary>
        /// Instance of the Message used for mafia
        /// </summary>
        private readonly DiscordMessage message;

        /// <summary>
        /// List of players playing mafia
        /// </summary>
        private readonly Player[] players;

        /// <summary>
        /// The time that each round should last for
        /// </summary>
        private TimeSpan roundTime = TimeSpan.FromMinutes(2);

        /// <summary>
        /// The status of the game
        /// </summary>
        private GameStatus gameStatus = GameStatus.Starting;

        /// <summary>
        /// The round the game is up to
        /// </summary>
        private int round = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mafia"/> class.
        /// </summary>
        /// <param name="mafiaCount">
        /// The number of mafia in the game
        /// </param>
        /// <param name="policeCount">
        /// The number of police in the game
        /// </param>
        /// <param name="haveDoctor">
        ///  Whether or not this game has a doctor
        /// </param>
        /// <param name="client">
        /// Instance of a Discord Client
        /// </param>
        /// <param name="message">
        /// Instance of the Message used for mafia
        /// </param>
        /// <param name="players">
        /// List of players playing mafia
        /// </param>
        public Mafia(int mafiaCount, int policeCount, bool haveDoctor, DiscordClient client, DiscordMessage message, Player[] players)
        {
            this.mafiaCount = mafiaCount;
            this.policeCount = policeCount;
            this.haveDoctor = haveDoctor;
            this.players = players;
            this.client = client;
            this.message = message;
        }

        /// <summary>
        /// Handles playing mafia
        /// </summary>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        public async Task Run()
        {
            await this.message.ModifyAsync(
                null,
                new DiscordEmbedBuilder
                    {
                        Title = "Mafia: Starting"
            });
            await this.SetupPlayers();
            await this.HandleDayVote();

        }

        /// <summary>
        /// Gives roles to players
        /// </summary>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private async Task SetupPlayers()
        {
            var roleList = new List<Role>();
            for (var i = 0; i < this.mafiaCount; i++)
            {
                roleList.Add(Role.Mafia);
            }
            for (var i = 0; i < this.policeCount; i++)
            {
                roleList.Add(Role.Police);
            }
            if (this.haveDoctor)
            {
                roleList.Add(Role.Doctor);
            }

            this.players.Shuffle();

            var mafia = new List<Player>();
            var police = new List<Player>();
            Player doctor = null;

            for (var i = 0; i < roleList.Count; i++)
            {
                var newRole = roleList[i];
                var player = this.players[i];
                player.Role = newRole;

                switch (newRole)
                {
                    case Role.Mafia:
                        mafia.Add(player);
                        break;
                    case Role.Police:
                        police.Add(player);
                        break;
                    case Role.Doctor:
                        doctor = player;
                        break;
                }
            }

            await this.AlertPlayerRoles(mafia, police, doctor);

            this.gameStatus = GameStatus.Day;
        }

        /// <summary>
        /// Alerts players to there roles
        /// </summary>
        /// <param name="mafia">
        /// List of players who are mafia
        /// </param>
        /// <param name="police">
        /// List of players who are police
        /// </param>
        /// <param name="doctor">
        /// Player who is the doctor
        /// </param>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private async Task AlertPlayerRoles(List<Player> mafia, List<Player> police, Player doctor)
        {
            var mafiaEmbed = new DiscordEmbedBuilder
            {
                Title = "Mafia: Role Announcement",
                Description = "You are " + Role.Mafia.GetRoleWithPrefix(this.mafiaCount, this.policeCount) +
                              (this.mafiaCount > 1 ? ", Other members are:" : string.Empty)
            };
            var policeEmbed = new DiscordEmbedBuilder
            {
                Title = "Mafia: Role Announcement",
                Description = "You are " + Role.Mafia.GetRoleWithPrefix(this.mafiaCount, this.policeCount) +
                              (this.policeCount > 1 ? ", Other members are:" : string.Empty)
            };
            var doctorEmbed = new DiscordEmbedBuilder
            {
                Title = "Mafia: Role Announcement",
                Description = "You are " + Role.Mafia.GetRoleWithPrefix(this.mafiaCount, this.policeCount)
            };

            mafia.ForEach(p => mafiaEmbed.AddField("Mafia Member", p.DiscordUser.Username));
            police.ForEach(p => policeEmbed.AddField("Police Member", p.DiscordUser.Username));

            mafia.ForEach(async p =>
            {
                var playerChat = await this.client.CreateDmAsync(p.DiscordUser);
                await playerChat.SendMessageAsync(null, false, mafiaEmbed);
            });
            police.ForEach(async p =>
            {
                var playerChat = await this.client.CreateDmAsync(p.DiscordUser);
                await playerChat.SendMessageAsync(null, false, policeEmbed);
            });
            if (this.haveDoctor)
            {
                var playerChat = await this.client.CreateDmAsync(doctor.DiscordUser);
                await playerChat.SendMessageAsync(null, false, doctorEmbed);
            }
        }

        // TODO: modify to handle all voting

        /// <summary>
        /// Handles voting during the day
        /// </summary>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private async Task HandleDayVote()
        {
            await this.CreateDayVoteEmbed();

            Thread.Sleep(TimeSpan.FromMinutes(1));

            var mostVotedPlayer = this.players.First();

            for (var playerPosition = 'a'; playerPosition - 'a' < this.players.Length; playerPosition++)
            {
                int playerVoteCount = (await this.message.GetReactionsAsync(
                                           DiscordEmoji.FromName(
                                               this.client,
                                               ":regional_indicator_" + playerPosition + ':'))).Count;

                var currentPlayer = this.players[playerPosition - 'a'];
                currentPlayer.Votes = playerVoteCount;
                if (mostVotedPlayer.Votes < playerVoteCount)
                {
                    mostVotedPlayer = this.players[playerPosition - 'a'];
                }
            }
        }

        /// <summary>
        /// Creates and sends the embedded message for the day vote
        /// </summary>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private async Task CreateDayVoteEmbed()
        {
            var dayEmbed = new DiscordEmbedBuilder
            {
                Title = "Mafia: Day: Voting",
                Description = "Please vote for someone you think the mafia is",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Please click the letter that matches a player the player"
                }
            };

            var playerPosition = 'A';
            foreach (var player in this.players)
            {
                dayEmbed.AddField("player " + playerPosition, player.DiscordUser.Username);
                playerPosition++;
            }

            await this.message.ModifyAsync(null, dayEmbed);

            for (playerPosition = 'a'; playerPosition - 'a' < this.players.Length; playerPosition++)
            {
                await this.message.CreateReactionAsync(
                    DiscordEmoji.FromName(this.client, ":regional_indicator_" + playerPosition + ':'));
            }
        }
    }
}