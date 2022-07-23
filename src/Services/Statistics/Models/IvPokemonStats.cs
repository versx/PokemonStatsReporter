namespace PokemonStatsReporter.Services.Statistics.Models
{
    internal class IvPokemonStats
    {
        public uint PokemonId { get; set; }

        public double IV { get; set; }

        public ulong Count { get; set; }

        public ulong Total { get; set; }
    }
}