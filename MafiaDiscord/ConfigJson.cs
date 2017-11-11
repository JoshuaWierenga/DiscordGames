using Newtonsoft.Json;

namespace MafiaDiscord
{
    internal class ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}