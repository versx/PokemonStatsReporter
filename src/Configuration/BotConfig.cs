﻿namespace PokemonStatsReporter.Configuration
{
    using System.Text.Json.Serialization;

    public class BotConfig
    {
        /// <summary>
        /// Gets or sets the command prefix for all Discord commands
        /// </summary>
        [JsonPropertyName("commandPrefix")]
        public string CommandPrefix { get; set; }

        /// <summary>
        /// Gets or sets the guild id
        /// </summary>
        [JsonPropertyName("guildId")]
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the Discord bot token
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the Discord bot's custom status
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the owner id
        /// </summary>
        [JsonPropertyName("ownerId")]
        public ulong OwnerId { get; set; }
    }
}