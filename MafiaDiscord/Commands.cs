using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MafiaDiscord
{
    [Group("mafia", CanInvokeWithoutSubcommand = false)]
    [Description("Handles playing mafia")]
    public class MafiaCommands
    {
        private const int MafiaCount = 0;
        private const int PoliceCount = 0;
        private const int DoctorCount = 0;

        private TimeSpan _roundTime = TimeSpan.FromMinutes(2);
        private GameStatus _gameStatus = GameStatus.Stopped;
        private int _round = 1;

        [Command("start")]
        [Description("Starts a game of mafia")]
        public async Task Start(CommandContext ctx)
        {
            var joinEmoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            var startEmoji = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");

            var embed = new DiscordEmbedBuilder {Title = "Players"};

            var msg = await ctx.RespondAsync(embed: embed);
            await msg.CreateReactionAsync(joinEmoji);

            var players = new string[1];
            var allowStartGame = false;
            var startGame = false;

            //used instead of thread sleep so function in timer can interupt sleep
            var sleeper = new BlockingCollection<int>();

            //run once every half a second
            var joinTimer = new Timer(async e =>
            {
                var currentPlayers = (await msg.GetReactionsAsync(joinEmoji))
                    .Where(p => p.Username != ctx.Client.CurrentUser.Username)
                    .Select(p => p.Username).ToArray();

                embed.Description = String.Join('\n', currentPlayers) + "\n\nClick the join button to join the game";

                if (currentPlayers.Length > 1)
                {
                    embed.Description += "\nGame now has enough players to start, Click the start button to begin";

                    if (!allowStartGame)
                    {
                        allowStartGame = true;                       
                        await msg.CreateReactionAsync(startEmoji);
                    }

                    int startRequests = (await msg.GetReactionsAsync(startEmoji))
                        .Count(p => p.Username != ctx.Client.CurrentUser.Username);
                    if (startRequests > 0)
                    {
                        //interupt sleep
                        sleeper.Add(0);
                        startGame = true;
                        return;
                    }
                }
                else if (allowStartGame)
                {
                    allowStartGame = false;
                    foreach (var discordUser in await msg.GetReactionsAsync(startEmoji))
                    {
                        await msg.DeleteReactionAsync(startEmoji, discordUser, "Game does not have enough users to start");
                    }
                }

                if (players.SequenceEqual(currentPlayers)) return;

                players = currentPlayers;
                await msg.ModifyAsync(embed: embed);
            }, null, 500, 500);

            //sleep for 2 minutes
            sleeper.TryTake(out _, TimeSpan.FromMinutes(2));
            joinTimer.Change(-1, -1);

            await msg.DeleteAllReactionsAsync("joining is over");
            await msg.ModifyAsync(startGame ? "game started" : "game canceled", null);
        }
    }
}