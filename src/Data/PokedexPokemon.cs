namespace StatsReporter.Data
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class PokedexPokemon
    {
        [JsonPropertyName("pokedex_id")]
        public uint PokedexId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("forms")]
        public Dictionary<uint, PokedexPokemon> Forms { get; set; } = new();

        [JsonPropertyName("form")]
        public ushort? Form { get; set; }

        [JsonPropertyName("types")]
        public List<PokemonType> Types { get; set; } = new();

        [JsonPropertyName("gen_id")]
        public uint GenerationId { get; set; }

        [JsonPropertyName("generation")]
        public string Generation { get; set; }
    }
}