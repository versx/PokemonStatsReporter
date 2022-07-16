namespace StatsReporter.Configuration
{
    using System.Text.Json.Serialization;

    using PokemonStatsReporter.Configuration;

    public class DailyStatsConfig
    {
        [JsonPropertyName("shiny")]
        public StatsConfig ShinyStats { get; set; } = new();

        [JsonPropertyName("hundo")]
        public StatsConfig HundoStats { get; set; } = new();

        [JsonPropertyName("iv")]
        public IvStatsConfig IvStats { get; set; } = new();
    }
}