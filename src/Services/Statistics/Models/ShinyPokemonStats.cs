namespace StatsReporter.Services.Statistics.Models
{
    internal class ShinyPokemonStats
    {
        public uint PokemonId { get; set; }

        public ulong Shiny { get; set; }

        public ulong Total { get; set; }
    }
}