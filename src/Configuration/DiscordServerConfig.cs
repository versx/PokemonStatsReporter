namespace PokemonStatsReporter.Configuration
{
    using System.Text.Json.Serialization;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Discord server configuration class
    /// </summary>
    public class DiscordServerConfig
    {
        /// <summary>
        /// Gets or sets the bot configuration to use
        /// </summary>
        [JsonPropertyName("bot")]
        public BotConfig Bot { get; set; }

        //[JsonProperty("locale")]
        //public string Locale { get; set; }

        /// <summary>
        /// Gets or sets the daily stats configuration for nightly channel postings
        /// </summary>
        [JsonPropertyName("dailyStats")]
        public DailyStatsConfig DailyStats { get; set; } = new();

        /// <summary>
        /// Gets or sets the DiscordClient minimum log level to use for the DSharpPlus
        /// internal logger (separate from the main logs)
        /// </summary>
        [JsonPropertyName("logLevel")]
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Instantiate a new <see cref="DiscordServerConfig"/> class
        /// </summary>
        public DiscordServerConfig()
        {
            LogLevel = LogLevel.Error;
        }
    }
}