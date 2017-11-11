// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="n/a">
//   n/a
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MafiaDiscord
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Exceptions;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;
    using DSharpPlus.Interactivity;

    using Newtonsoft.Json;

    /// <summary>
    /// Main class for the discord bot
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Instance of a discord client
        /// </summary>
        private static DiscordClient client;

        /// <summary>
        /// Instance of a CommandNext Module
        /// </summary>
        private static CommandsNextModule commands;

        /// <summary>
        /// Entry point for the discord bot
        /// </summary>
        /// <param name="args">
        /// Command line arguments
        /// </param>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private static async Task Main(string[] args)
        {
            string json;
            using (var fs = File.OpenRead("config.json"))
            {
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    json = await sr.ReadToEndAsync();
                }
            }
            var config = JsonConvert.DeserializeObject<ConfigJson>(json);

            client = new DiscordClient(new DiscordConfiguration
            {
                Token = config.Token,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            });
            client.GuildAvailable += GuildAvailable;
            client.ClientErrored += ClientErrored;
            client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = TimeoutBehaviour.Ignore,
                PaginationTimeout = TimeSpan.FromMinutes(5),
                Timeout = TimeSpan.FromMinutes(2)
            });

            commands = client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = config.CommandPrefix
            });
            commands.CommandExecuted += CommandExecuted;
            commands.CommandErrored += CommandErrored;
            commands.RegisterCommands<Commands>();

            await client.ConnectAsync();
            await Task.Delay(-1);
        }

        /// <summary>
        /// callback for when the bot connects a discord guild
        /// </summary>
        /// <param name="eventArgs">
        /// arguments relating to the connection
        /// </param>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private static Task GuildAvailable(GuildCreateEventArgs eventArgs)
        {
            eventArgs.Client.DebugLogger.LogMessage(
                LogLevel.Info,
                "Mafia Bot",
                $"Guild available: {eventArgs.Guild.Name}",
                DateTime.Now);
            return Task.CompletedTask;
        }

        /// <summary>
        /// callback for when the bot has an error
        /// </summary>
        /// <param name="eventArgs">
        /// arguments relating to the error
        /// </param>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private static Task ClientErrored(ClientErrorEventArgs eventArgs)
        {
            eventArgs.Client.DebugLogger.LogMessage(
                LogLevel.Error,
                "Mafia Bot",
                $"Exception occurred: {eventArgs.Exception.GetType()}: {eventArgs.Exception.Message}",
                DateTime.Now);
            return Task.CompletedTask;
        }

        /// <summary>
        /// callback for when the bot has finished executing a command next command
        /// </summary>
        /// <param name="eventArgs">
        /// arguments relating to the command
        /// </param>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private static Task CommandExecuted(CommandExecutionEventArgs eventArgs)
        {
            eventArgs.Context.Client.DebugLogger.LogMessage(
                LogLevel.Info,
                "Mafia Bot",
                $"{eventArgs.Context.User.Username} successfully executed '{eventArgs.Command.QualifiedName}'",
                DateTime.Now);
            return Task.CompletedTask;
        }

        /// <summary>
        /// callback for when the bot errors while executing a command next command
        /// </summary>
        /// <param name="eventArgs">
        /// arguments relating to the error
        /// </param>
        /// <returns>
        /// <see cref="Task"/>.
        /// </returns>
        private static async Task CommandErrored(CommandErrorEventArgs eventArgs)
        {
            string errorLog =
                $"{eventArgs.Context.User.Username} tried executing '{eventArgs.Command?.QualifiedName ?? "<unknown command>"}'"
                + $" but it errored: {eventArgs.Exception.GetType()}: {eventArgs.Exception.Message ?? "<no message>"}";

            // let's log the error details
            eventArgs.Context.Client.DebugLogger.LogMessage(
                LogLevel.Error,
                "Mafia Bot",
                errorLog,
                DateTime.Now);

            // let's check if the error is a result of lack
            // of required permissions
            if (eventArgs.Exception is ChecksFailedException ex)
            {
                // yes, the user lacks required permissions, 
                // let them know

                var emoji = DiscordEmoji.FromName(eventArgs.Context.Client, ":no_entry:");

                // let's wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await eventArgs.Context.RespondAsync("", embed: embed);
            }
        }

        /*private static async Task EndVoting()
        {
            int maxVote = Players.Select(player => player.Votes).Max();
            var playerMaxVotes = Players.Where(player => player.Votes == maxVote).ToList();
            if (playerMaxVotes.Count != 1) return;

            var votedPlayer = playerMaxVotes.First();
            votedPlayer.Alive = false;

            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                player.Votes = 0;
                player.Voted = false;
            }

            //await gameChannel.SendMessageAsync(votedPlayer.Name + " received " + votedPlayer.Votes
            //  + (votedPlayer.Votes == 1 ? " vote" : " votes") + " and is now dead, they were "
            //+ GetRolePrefix(votedPlayer.Role) + votedPlayer.Role.ToString().ToLower());
        }

        private static async Task HandleVote(Player voter, Player voted, bool privateVote)
        {
            //if (voter.Voted) await gameChannel.SendMessageAsync("You have already voted in this round");
            //if (_gameStatus == GameStatus.Day && privateVote) await _client.GetUser(voter.Discordid).SendMessageAsync("Plese vote via the group chat");
            //if (_gameStatus == GameStatus.Night & !privateVote) await gameChannel.SendMessageAsync("You can not vote at this time");
            //if (voter.Discordid == voted.Discordid)
            {
                if (privateVote)
                {
                    //await _client.GetUser(voter.Discordid).SendMessageAsync("You can not vote for your self");
                }
                else
                {
                    //await gameChannel.SendMessageAsync("You can not vote for your self");
                }

            }

            voted.Votes++;
            voter.Voted = true;

            //await gameChannel.SendMessageAsync("Your vote against " + voted.Name + " has been counted");
        }*/
    }
}