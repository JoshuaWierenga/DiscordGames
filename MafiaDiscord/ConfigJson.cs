// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigJson.cs" company="n\a">
//   n\a
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MafiaDiscord
{
    using Newtonsoft.Json;

    /// <summary>
    /// Object holding json properties for the config file
    /// </summary>
    internal class ConfigJson
    {
        /// <summary>
        /// Gets the discord bot token
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; private set; }

        /// <summary>
        /// Gets the command prefix.
        /// </summary>
        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}