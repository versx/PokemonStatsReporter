namespace PokemonStatsReporter.Configuration
{
    using System.Text.Json.Serialization;

    using StatsReporter.Configuration;

    public class IvStatsConfig : StatsConfig
    {
        [JsonPropertyName("minimumIV")]
        public double MinimumIV { get; set; }
    }
}