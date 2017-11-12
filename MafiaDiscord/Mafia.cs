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
        public async Task RunAsync()
        {
            await this.message.ModifyAsync(
                null,
                new DiscordEmbedBuilder
                {
                    Title = "Mafia: Starting"
                });
            await this.SetupPlayersAsync();
            await this.HandleDayAsync();

        }

        /// <summary>
        /// Gives roles to players
        /// </summary>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private async Task SetupPlayersAsync()
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
            var civilians = new List<Player>();
            Player doctor = null;

            for (var i = 0; i < this.players.Length; i++)
            {
                var player = this.players[i];
                var newRole = player.Role;
                if (i < roleList.Count)
                {
                    newRole = roleList[i];
                    player.Role = newRole;
                }

                switch (newRole)
                {
                    case Role.Mafia:
                        mafia.Add(player);
                        break;
                    case Role.Police:
                        police.Add(player);
                        break;
                    case Role.Civilian:
                        civilians.Add(player);
                        break;
                    case Role.Doctor:
                        doctor = player;
                        break;
                }
            }

            this.players.Shuffle();

            await this.AlertPlayerRolesAsync(mafia, police, civilians, doctor);
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
        /// <param name="civilians">
        /// List of players who are civilians
        /// </param>
        /// <param name="doctor">
        /// Player who is the doctor
        /// </param>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private async Task AlertPlayerRolesAsync(List<Player> mafia, List<Player> police, List<Player> civilians, Player doctor)
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
                Description = "You are " + Role.Police.GetRoleWithPrefix(this.mafiaCount, this.policeCount) +
                              (this.policeCount > 1 ? ", Other members are:" : string.Empty)
            };
            var civilianEmbed = new DiscordEmbedBuilder
            {
                Title = "Mafia: Role Announcement",
                Description = "You are " + Role.Civilian.GetRoleWithPrefix(this.mafiaCount, this.policeCount)
            };

            if (this.mafiaCount > 1)
            {
                mafia.ForEach(p => mafiaEmbed.AddField("Mafia Member", p.DiscordUser.Username));
            }
            if (this.policeCount > 1)
            {
                police.ForEach(p => policeEmbed.AddField("Police Member", p.DiscordUser.Username));
            }          

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
            civilians.ForEach(async p =>
            {
                var playerChat = await this.client.CreateDmAsync(p.DiscordUser);
                await playerChat.SendMessageAsync(null, false, civilianEmbed);
            });

            if (this.haveDoctor)
            {
                var doctorEmbed = new DiscordEmbedBuilder
                {
                    Title = "Mafia: Role Announcement",
                    Description = "You are" + Role.Doctor.GetRoleWithPrefix(this.mafiaCount, this.policeCount)
                };
                var playerChat = await this.client.CreateDmAsync(doctor.DiscordUser);
                await playerChat.SendMessageAsync(null, false, doctorEmbed);
            }
        }

        // TODO: modify to handle all voting
        // TODO: prevent multiple votes

        /// <summary>
        /// Handles voting during the day
        /// </summary>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private async Task HandleDayAsync()
        {
            await this.CreateDayVoteAsync();
            Thread.Sleep(TimeSpan.FromSeconds(30));

            var mostVotedPlayer = this.players.First();
            var remainingMafia = 0;
            var remainingNonMafia = 0;
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

                if (currentPlayer.Role == Role.Mafia)
                {
                    remainingMafia++;
                }
                else
                {
                    remainingNonMafia++;
                }
            }

            await this.message.DeleteAllReactionsAsync("Voting is over");

            mostVotedPlayer.Alive = false;
            if (mostVotedPlayer.Role == Role.Mafia)
            {
                remainingMafia--;
            }
            else
            {
                remainingNonMafia--;
            }

            await this.CreateDayVoteResultAsync(mostVotedPlayer);

            Thread.Sleep(TimeSpan.FromSeconds(5));

            if (remainingMafia == 0 || remainingNonMafia == 0)
            {
                await this.CreateGameOverAsync();
            }
        }

        /// <summary>
        /// Creates and sends the embedded message for the day vote
        /// </summary>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private async Task CreateDayVoteAsync()
        {
            var dayEmbed = new DiscordEmbedBuilder
            {
                Title = "Mafia: Day: Voting",
                Description = "Please vote for someone you think the mafia is",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Please click the letter that matches the player. Please only vote once, extra votes will be ignored"
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

        /// <summary>
        /// Creates and sends the embedded message for the day vote result
        /// </summary>
        /// <param name="votedPlayer">
        /// Player who has been voted off
        /// </param>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private async Task CreateDayVoteResultAsync(Player votedPlayer)
        {
            var dayResultEmbed = new DiscordEmbedBuilder
            {
                Title = "Mafia: Day: Vote Result",
                Description = "You voted for"
            };

            dayResultEmbed.AddField(
                votedPlayer.DiscordUser.Username,
                votedPlayer.Role.GetRoleWithPrefix(this.mafiaCount, this.policeCount));

            await this.message.ModifyAsync(null, dayResultEmbed);
        }

        /// <summary>
        /// Creates and sends the embedded message for game over
        /// </summary>
        /// <returns>
        /// <see cref="Task"/>
        /// </returns>
        private async Task CreateGameOverAsync()
        {
            var gameOverEmbed = new DiscordEmbedBuilder
            {
                Title = "Mafia: Game Over",
                Description = "Game is now over.\nPlayer Roles:"
            };

            foreach (var player in this.players)
            {
                gameOverEmbed.AddField(
                    player.DiscordUser.Username + " : " + (player.Alive ? "Alive" : "Dead"),
                    player.Role.GetRoleWithPrefix(this.mafiaCount, this.policeCount));
            }

            await this.message.ModifyAsync(null, gameOverEmbed);
        }
    }
}