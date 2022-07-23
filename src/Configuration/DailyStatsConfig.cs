namespace PokemonStatsReporter.Configuration
{
    using System.Text.Json.Serialization;

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