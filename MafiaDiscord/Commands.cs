// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Commands.cs" company="n\a">
//   n\a
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MafiaDiscord
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;

    /// <summary>
    /// Contains commands for commands next
    /// </summary>
    [Group("mafia", CanInvokeWithoutSubcommand = false)]
    [Description("Handles playing mafia")]
    public class Commands
    {
        /// <summary>
        /// Stores a reference to the current game of mafia
        /// </summary>
        private static Mafia currentGame;

        /// <summary>
        /// Commands next command called start
        /// </summary>
        /// <param name="ctx">
        /// The command context for the start command
        /// </param>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        [Command("play")]
        [Description("Starts a game of mafia")]
        public async Task Start(CommandContext ctx)
        {
            var joinEmoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            var startEmoji = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");

            var embed = new DiscordEmbedBuilder
            {
                Title = "Mafia: Joining",
                Description = "Click the join button to join the game"
            };

            var msg = await ctx.RespondAsync(embed: embed);
            await msg.CreateReactionAsync(joinEmoji);

            var players = new DiscordUser[1];
            var allowStartGame = false;
            var startGame = false;

            // used instead of thread sleep so function in timer can interrupt sleep
            var sleeper = new BlockingCollection<int>();

            // run once every half a second
            var joinTimer = new Timer(
                async e =>
                    {
                        var currentPlayers = (await msg.GetReactionsAsync(joinEmoji))
                        .Where(p => p.Id != ctx.Client.CurrentUser.Id).ToArray();

                        if (currentPlayers.Length <= 1 && allowStartGame)
                        {
                            allowStartGame = false;
                            foreach (var discordUser in await msg.GetReactionsAsync(startEmoji))
                            {
                                await msg.DeleteReactionAsync(
                                    startEmoji,
                                    discordUser,
                                    "Game does not have enough users to start");
                            }
                        }

                        if (!players.SequenceEqual(currentPlayers))
                        {
                            embed.Description = string.Join('\n', currentPlayers.Select(p => p.Username))
                                                + "\n\nClick the join button to join the game";
                            if (currentPlayers.Length > 1)
                            {
                                embed.Description += "\nGame now has enough players to start, Click the start button to begin";
                            }

                            players = currentPlayers;
                            await msg.ModifyAsync(embed: embed);
                        }

                        if (currentPlayers.Length <= 1)
                        {
                            return;
                        }

                        if (!allowStartGame)
                        {
                            allowStartGame = true;
                            await msg.CreateReactionAsync(startEmoji);
                        }

                        int startRequests = (await msg.GetReactionsAsync(startEmoji))
                            .Count(p => p.Username != ctx.Client.CurrentUser.Username);
                        if (startRequests <= 0)
                        {
                            return;
                        }

                        // interrupt sleep
                        sleeper.Add(0);
                        startGame = true;
                    },
                null,
                500,
                500);

            // sleep for 2 minutes
            sleeper.TryTake(out _, TimeSpan.FromMinutes(2));
            joinTimer.Change(-1, -1);

            await msg.DeleteAllReactionsAsync("joining is over");

            if (!startGame)
            {
                await msg.ModifyAsync(null, new DiscordEmbedBuilder
                {
                    Title = "Mafia: Game Canceled"
                });
                return;
            }

            currentGame = new Mafia(1, 0, false, ctx.Client, msg, players.Select(p => new Player(p)).ToArray());
            await currentGame.Run();
        }
    }
}