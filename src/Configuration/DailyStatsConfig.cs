namespace StatsReporter.Configuration
{
    using System.Text.Json.Serialization;

    public class DailyStatsConfig
    {
        [JsonPropertyName("shiny")]
        public StatsConfig ShinyStats { get; set; } = new();

        [JsonPropertyName("iv")]
        public StatsConfig IVStats { get; set; } = new();
    }
}