using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;

namespace MafiaDiscord
{
    internal class Program
    {
        private static readonly List<Player> Players = new List<Player>();

        private static DiscordClient _client;
        private static CommandsNextModule _commands;

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

            _client = new DiscordClient(new DiscordConfiguration
            {
                Token = config.Token,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            });
            _client.GuildAvailable += GuildAvailable;
            _client.ClientErrored += ClientErrored;
            _client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour =  TimeoutBehaviour.Ignore,
                PaginationTimeout = TimeSpan.FromMinutes(5),
                Timeout = TimeSpan.FromMinutes(2)
            });

            _commands = _client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = config.CommandPrefix
            });
            _commands.CommandExecuted += CommandExecuted;
            _commands.CommandErrored += CommandErrored;
            _commands.RegisterCommands<MafiaCommands>();

            await _client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static Task GuildAvailable(GuildCreateEventArgs eventArgs)
        {
            eventArgs.Client.DebugLogger.LogMessage(LogLevel.Info, "Mafia Bot", 
                $"Guild available: {eventArgs.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private static Task ClientErrored(ClientErrorEventArgs eventArgs)
        {
            eventArgs.Client.DebugLogger.LogMessage(LogLevel.Error, "Mafia Bot", 
                $"Exception occured: {eventArgs.Exception.GetType()}: {eventArgs.Exception.Message}", DateTime.Now);
            return Task.CompletedTask;
        }

        private static Task CommandExecuted(CommandExecutionEventArgs eventArgs)
        {
            eventArgs.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "Mafia Bots", 
                $"{eventArgs.Context.User.Username} successfully executed '{eventArgs.Command.QualifiedName}'", DateTime.Now);
            return Task.CompletedTask;
        }

        private static async Task CommandErrored(CommandErrorEventArgs eventArgs)
        {
            // let's log the error details
            eventArgs.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "Mafia Bot",
                $"{eventArgs.Context.User.Username} tried executing '{eventArgs.Command?.QualifiedName ?? "<unknown command>"}'" +
                $" but it errored: {eventArgs.Exception.GetType()}: {eventArgs.Exception.Message ?? "<no message>"}", DateTime.Now);

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

        

        /*private static async Task MessageReceived(SocketMessage message)
        {
            switch (message.Content)
            {
                case "!mafia start" when message.Channel is SocketGuildChannel && _gameStatus == GameStatus.Stopped:
                    {
                        _gameStatus = GameStatus.Starting;
                        await message.Channel.SendMessageAsync(
                            "Game is starting, send !mafia join to join the game");
                        break;
                    }
                case "!mafia start" when message.Channel is SocketGuildChannel && _gameStatus == GameStatus.Starting:
                    {
                        if (Players.Count <= MafiaCount + PoliceCount)
                        {
                            await message.Author.SendMessageAsync("This game does not have enough users to start");
                        }

                        var random = new Random();

                        var newMafia = Players.GetRange(random.Next(0, Players.Count - MafiaCount), MafiaCount)
                                              .Select(p => { p.Role = Role.Mafia; return p; }).ToList();
                        Players.GetRange(random.Next(0, Players.Count - PoliceCount), PoliceCount).ForEach(p => p.Role = Role.Doctor);
                        Players.GetRange(random.Next(0, Players.Count - 1), 1)
                               .Select(p => { p.Role = Role.Doctor; return p; }).ToList();



                        /*var mafiaMessage = "Mafia members:\n";
                        var policeMessage = "Police members:\n";

                        var i = 0;
                        foreach (var role in Roles)
                        {
                            Players[i].Role = role;
                            if (role == Role.Mafia)
                            {
                                mafiaMessage += Players[i].Name + '\n';
                            }
                            else if (role == Role.Police)
                            {
                                policeMessage += Players[i].Name + '\n';
                            }
                            i++;
                        }

                        foreach (var player in Players)
                        {
                            var playerDiscordUser = Client.GetUser(player.Discordid);

                            string userMessage = "You are "
                                                 + GetRolePrefix(player.Role)
                                                 + player.Role.ToString().ToLower();

                            await playerDiscordUser.SendMessageAsync(userMessage);

                            if (player.Role == Role.Mafia)
                            {
                                await playerDiscordUser.SendMessageAsync(mafiaMessage);
                            }
                            else if (player.Role == Role.Police)
                            {
                                await playerDiscordUser.SendMessageAsync(policeMessage);
                            }
                        }

                        break;
                    }
                case "!mafia join" when message.Channel is SocketGuildChannel && _gameStatus == GameStatus.Starting:
                    {
                        if (Players.FirstOrDefault(p => p.Discordid == message.Author.Id) != null)
                        {
                            await message.Channel.SendMessageAsync("You are already in this game");
                            break;
                        }

                        Players.Add(new Player(message.Author.Id, message.Author.Username, Role.Civilian));

                        if (Players.Count > MafiaCount + PoliceCount + DoctorCount)
                        {
                            await message.Channel.SendMessageAsync(
                                "game has enough people to start, resend !mafia start to start the game");
                        }
                        break;
                    }
            }
        }*/

        private static async Task EndVoting()
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

        private static async Task StartDay()
        {
            var startMessage = "Alive players:\n";

            foreach (var player in Players.Where(p => p.Alive))
            {
                startMessage += player.Name + '\n';
            }

            //await gameChannel.SendMessageAsync(startMessage + "Please vote for who you think the mafia are, send !mafia continue when you are done");
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
        }

        private static string GetRoleWithPrefix(Role role)
        {
            switch (role)
            {
                case Role.Mafia:
                case Role.Police:
                    return "a member of the " + role;
                case Role.Doctor:
                    return "the " + role;
                case Role.Civilian:
                    return "a " + role;
                default:
                    return role.ToString();
            }
        }
    }
}