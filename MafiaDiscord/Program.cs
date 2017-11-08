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
        private static GameStatus gameStatus;
        private static bool _nightTime;
        private static int round = 1;

        private static List<Player> _players = new List<Player>();

        private static List<Role> _roles = new List<Role>
        {
            Role.Mafia
        };

        private static readonly Random Random = new Random();

        private static readonly DiscordSocketClient Client = new DiscordSocketClient();

        private static async Task Main()
        {
            Client.Log += Log;
            Client.MessageReceived += MessageReceived;

            const string token = "MzIyMjYxMzI3NzAxNjA2NDAw.DOSR3g.rW36NqMJ7GUTcV8lMA_3-mHqwyU";
            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();
            await Task.Delay(-1);

            /*while (true)
            {
                switch (Console.ReadLine())
                {
                    case "start game":
                        {
                            _users = _users.OrderBy(x => _random.Next()).ToList();

                            var i = 0;
                            foreach (var role in _roles)
                            {
                                _players.Add(new Player(_users[i], role));
                                i++;
                            }
                            while (i < _users.Count)
                            {
                                _players.Add(new Player(_users[i], Role.Civilian));
                                i++;
                            }

                            _gameStarted = true;

                            break;
                        }

                    case "status":
                        {
                            Console.WriteLine("game is " + (_gameStarted ? "" : "not ") + "in progress");
                            if (_gameStarted)
                            {
                                foreach (var player in _players)
                                {
                                    if (player.Alive)
                                    {
                                        Console.WriteLine(player.Name + " is alive and has the " 
                                            + player.Role + " role and has "
                                            + player.Votes + " votes against them");
                                    }
                                    else
                                    {
                                        Console.WriteLine( player.Name + " is dead and had the " + player.Role + " role");
                                    }

                                }
                            }
                            break;
                        }

                    case string input when input.StartsWith("vote"):
                        {
                            if (_gameStarted && !_nightTime)
                            {
                                var splitInput = input.Remove(0, 5).Split(' ');
                                var voter = _players.Find(p => p.Name == splitInput[0]);
                                if (voter != null && !voter.Voted)
                                {
                                    var voted = _players.FirstOrDefault(p => p.Name == splitInput[1]);
                                    if (voted != null)
                                    {
                                        voted.Votes++;
                                        voter.Voted = true;
                                    }
                                }
                            }
                            break;
                        }


                    case "end voting":
                        {
                            if (_gameStarted && !_nightTime)
                            {
                                int playerMaxVote = _players.Select(player => player.Votes).Max();
                                var playerMaxVotes = _players.Where(player => player.Votes == playerMaxVote).ToList();
                                if (playerMaxVotes.Count == 1)
                                {
                                    var votedPlayer = playerMaxVotes.First();
                                    votedPlayer.Alive = false;
                                    Console.WriteLine(votedPlayer.Name + " is now dead, their role was " + votedPlayer.Role);
                                }

                                for (var i = 0; i < _players.Count; i++)
                                {
                                    var player = _players[i];
                                    player.Votes = 0;
                                    player.Voted = false;
                                }
                            }
                            break;
                        }


                    case "continue game":
                        {
                            break;
                        }

                }
            }*/

        }

        private static async Task MessageReceived(SocketMessage message)
        {
            switch (message.Content)
            {
                case "!mafia start":
                    {
                        switch (gameStatus)
                        {
                            case GameStatus.Stopped:
                                {
                                    gameStatus = GameStatus.Starting;
                                    await message.Channel.SendMessageAsync("Game is starting, send !mafia join to join the game");
                                    break;
                                }
                            case GameStatus.Starting:
                                {
                                    if (_players.Count <= _roles.Count)
                                    {
                                        await message.Author.SendMessageAsync("This game does not have enough users to start");
                                    }

                                    var i = 0;
                                    string startMessage = "Game has started \n" +
                                                       "players:\n";
                                    var mafiaMessage = "Mafia members:\n";
                                    var policeMessage = "Police members:\n";

                                    foreach (var role in _roles)
                                    {
                                        _players[i].Role = role;
                                        startMessage += _players[i].Name + '\n';
                                        if (role == Role.Mafia)
                                        {
                                            mafiaMessage += _players[i].Name + '\n';
                                        }
                                        else if (role == Role.Police)
                                        {
                                            policeMessage += _players[i].Name + '\n';
                                        }
                                        i++;
                                    }

                                    for (; i < _players.Count; i++)
                                    {
                                        startMessage += _players[i].Name + '\n';
                                    }

                                    foreach (var player in _players)
                                    {
                                        var playerDiscordUser = Client.GetUser(player.Discordid);

                                        string userMessage = "You are ";

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

                                    gameStatus = GameStatus.Day;

                                    await message.Channel.SendMessageAsync(startMessage);
                                    break;
                                }
                        }
                        break;
                    }
                case "!mafia join":
                    {
                        if (gameStatus != GameStatus.Starting)
                        {
                            await message.Author.SendMessageAsync("The game is not currently starting");
                            break;
                        }
                        if (_players.FirstOrDefault(p => p.Discordid == message.Author.Id) != null)
                        {
                            await message.Author.SendMessageAsync("You are already part of this game");
                            break;
                        }

                        _players.Add(new Player(message.Author.Id, message.Author.Username, Role.Civilian));

                        if (_players.Count > _roles.Count)
                        {
                            await message.Channel.SendMessageAsync("game has enough people to start, resend !mafia start to start the game");
                        }

                        break;
                    }
                case "!mafia status":
                    {
                        string statusMessage;

                        if (gameStatus == GameStatus.Starting || gameStatus == GameStatus.Stopped)
                        {
                            statusMessage = "game is " + gameStatus.ToString().ToLower() + '\n';
                        }
                        else
                        {
                            statusMessage = "game is in a " + gameStatus.ToString().ToLower() + " phase\n";
                        }
                        if (gameStatus != GameStatus.Stopped)
                        {
                            foreach (var player in _players)
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
                        var voterPlayer = _players.Find(p => p.Discordid == message.Author.Id);

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
                        if (gameStatus != GameStatus.Day)
                        {
                            await message.Author.SendMessageAsync("You can only vote during the day");
                            break;
                        }

                        ulong votedId = message.MentionedUsers.FirstOrDefault()?.Id ?? 0;
                        var votedPlayer = _players.Find(p => p.Discordid == votedId);

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
                        votedPlayer.Voted = true;
                        break;
                    }
            }
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
