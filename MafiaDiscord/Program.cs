using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MafiaDiscord
{
    internal class Program
    {
        private const int RoundTime = 60000;

        private static GameStatus _gameStatus;
        private static GameStatus _gameNightStatus;
        private static int round = 1;

        private static readonly List<Player> Players = new List<Player>();

        private static readonly List<Role> Roles = new List<Role>
        {
            Role.Mafia
        };

        private static readonly DiscordSocketClient Client = new DiscordSocketClient();

        private static async Task Main(string[] args)
        {
            Client.Log += Log;
            Client.MessageReceived += MessageReceived;

            string token = args[0] ?? "";
            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();
            await Task.Delay(-1);
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private static async Task MessageReceived(SocketMessage message)
        {
            if (message.Channel is SocketGuildChannel)
            {
                switch (message.Content)
                {
                    case "!mafia start":
                    {
                        switch (_gameStatus)
                        {
                            case GameStatus.Stopped:
                            {
                                _gameStatus = GameStatus.Starting;
                                await message.Channel.SendMessageAsync(
                                    "Game is starting, send !mafia join to join the game");
                                break;
                            }
                            case GameStatus.Starting:
                            {
                                if (Players.Count <= Roles.Count)
                                {
                                    await message.Author.SendMessageAsync(
                                        "This game does not have enough users to start");
                                }

                                var i = 0;
                                string startMessage = "Game has started \n" +
                                                      "players:\n\n" +
                                                      "Please vote for who you think the mafia are, send !mafia continue when you are done";
                                var mafiaMessage = "Mafia members:\n";
                                var policeMessage = "Police members:\n";

                                foreach (var role in Roles)
                                {
                                    Players[i].Role = role;
                                    startMessage += Players[i].Name + '\n';
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

                                for (; i < Players.Count; i++)
                                {
                                    startMessage += Players[i].Name + '\n';
                                }

                                foreach (var player in Players)
                                {
                                    var playerDiscordUser = Client.GetUser(player.Discordid);

                                    var userMessage = "You are ";

                                    switch (player.Role)
                                    {
                                        case Role.Mafia:
                                        case Role.Police:
                                            userMessage += "a member of the ";
                                            break;
                                        case Role.Doctor:
                                            userMessage += "the ";
                                            break;
                                        case Role.Civilian:
                                            userMessage += "a ";
                                            break;
                                    }

                                    userMessage += player.Role.ToString().ToLower();

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

                                _gameStatus = GameStatus.Day;

                                await message.Channel.SendMessageAsync(startMessage);
                                break;
                            }
                        }
                        break;
                    }
                    case "!mafia join":
                    {
                        if (_gameStatus != GameStatus.Starting)
                        {
                            await message.Author.SendMessageAsync("The game is not currently starting");
                            break;
                        }
                        if (Players.FirstOrDefault(p => p.Discordid == message.Author.Id) != null)
                        {
                            await message.Author.SendMessageAsync("You are already part of this game");
                            break;
                        }

                        Players.Add(new Player(message.Author.Id, message.Author.Username, Role.Civilian));

                        if (Players.Count > Roles.Count)
                        {
                            await message.Channel.SendMessageAsync(
                                "game has enough people to start, resend !mafia start to start the game");
                        }

                        break;
                    }
                    case "!mafia status":
                    {
                        string statusMessage;

                        if (_gameStatus == GameStatus.Starting || _gameStatus == GameStatus.Stopped)
                        {
                            statusMessage = "game is " + _gameStatus.ToString().ToLower() + '\n';
                        }
                        else
                        {
                            statusMessage = "game is in a " + _gameStatus.ToString().ToLower() + " phase\n";
                        }
                        if (_gameStatus != GameStatus.Stopped)
                        {
                            foreach (var player in Players)
                            {
                                statusMessage += player.Name;

                                if (player.Alive)
                                {
                                    statusMessage += " is alive, has the "
                                                     + player.Role + " role and has "
                                                     + player.Votes + " votes against them";
                                }
                                else
                                {
                                    statusMessage += " is dead and had the " + player.Role + " role";
                                }

                                statusMessage += '\n';
                            }
                        }

                        await message.Channel.SendMessageAsync(statusMessage);
                        break;
                    }
                    case string input when input.StartsWith("!mafia vote"):
                    {
                        var voterPlayer = Players.Find(p => p.Discordid == message.Author.Id);

                        if (voterPlayer == null)
                        {
                            await message.Author.SendMessageAsync("You are not part of the game and can not vote");
                            return;
                        }
                        if (voterPlayer.Voted)
                        {
                            await message.Author.SendMessageAsync("You have already voted in this round");
                            return;
                        }
                        if (_gameStatus != GameStatus.Day)
                        {
                            await message.Author.SendMessageAsync("You can only vote during the day");
                            break;
                        }

                        ulong votedId = message.MentionedUsers.FirstOrDefault()?.Id ?? 0;
                        var votedPlayer = Players.Find(p => p.Discordid == votedId);

                        if (votedPlayer == null)
                        {
                            await message.Author.SendMessageAsync(
                                "The person you voted against is not part of this game");
                            break;
                        }
                        if (voterPlayer.Discordid == votedPlayer.Discordid)
                        {
                            await message.Author.SendMessageAsync("You can not vote against your self");
                            break;
                        }

                        votedPlayer.Votes++;
                        voterPlayer.Voted = true;
                        break;
                    }
                    case "!mafia continue":
                    {
                        await EndVoting(message.Channel);
                        _gameStatus = _gameStatus == GameStatus.Day ? GameStatus.Night : GameStatus.Day;
                        _gameNightStatus = GameStatus.NightMafia;
                        break;
                    }                     
                }
            }
            else
            {
                switch (message.Content)
                {
                    case string input when input.StartsWith("!mafia vote"):
                        if ()
                        break;
                }
            }
        }

        private static async Task EndVoting(ISocketMessageChannel channel)
        {
            if (_gameStatus != GameStatus.Day)
            {
                return;
            }

            int maxVote = Players.Select(player => player.Votes).Max();
            var playerMaxVotes = Players.Where(player => player.Votes == maxVote).ToList();
            if (playerMaxVotes.Count == 1)
            {
                var votedPlayer = playerMaxVotes.First();
                votedPlayer.Alive = false;

                string deathMessage = votedPlayer.Name + " received " + votedPlayer.Votes
                    + (votedPlayer.Votes == 1 ? " vote" : " votes") + " and is now dead, they were ";

                switch (votedPlayer.Role)
                {
                    case Role.Mafia:
                    case Role.Police:
                        deathMessage += "a member of the ";
                        break;
                    case Role.Doctor:
                        deathMessage += "the ";
                        break;
                    case Role.Civilian:
                        deathMessage += "a ";
                        break;
                }

                deathMessage += votedPlayer.Role.ToString().ToLower();

                await channel.SendMessageAsync(deathMessage);

                for (var i = 0; i < Players.Count; i++)
                {
                    var player = Players[i];
                    player.Votes = 0;
                    player.Voted = false;
                }

            }

        }
    }
}