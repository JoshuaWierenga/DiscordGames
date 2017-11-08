namespace MafiaDiscord
{
    internal class Player
    {
        public ulong Discordid;
        public string Name;
        public Role Role;
        public int Votes;
        public bool Voted;
        public bool Alive;

        public Player(ulong discordId, string name, Role role)
        {
            Discordid = discordId;
            Name = name;
            Role = role;
            Votes = 0;
            Voted = false;
            Alive = true;
        }
    }
}