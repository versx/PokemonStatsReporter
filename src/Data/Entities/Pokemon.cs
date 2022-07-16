namespace PokemonStatsReporter.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using PokemonStatsReporter.Extensions;

    [Table("pokemon")]
    public sealed class Pokemon
    {
        #region Properties

        [
            JsonPropertyName("pokemon_id"),
            Column("pokemon_id"),
        ]
        public uint PokemonId { get; set; }

        [
            JsonPropertyName("form"),
            Column("form"),
        ]
        public uint FormId { get; set; }

        [
            JsonPropertyName("costume"),
            Column("costume"),
        ]
        public uint CostumeId { get; set; }

        [
            JsonIgnore,
            NotMapped,
        ]
        public double IVReal => this.CalculateIV();

        [
            JsonPropertyName("individual_stamina"),
            Column("sta_iv"),
        ]
        public ushort? Stamina { get; set; }

        [
            JsonPropertyName("individual_attack"),
            Column("atk_iv"),
        ]
        public ushort? Attack { get; set; }

        [
            JsonPropertyName("individual_defense"),
            Column("def_iv"),
        ]
        public ushort? Defense { get; set; }

        [
            JsonPropertyName("disappear_time"),
            Column("expire_timestamp"),
        ]
        public long DisappearTime { get; set; }

        [
            JsonIgnore,
            NotMapped,
        ]
        public bool IsDitto => PokemonId == 132;

        [
            JsonIgnore,
            NotMapped,
        ]
        public bool IsMissingStats =>
            Attack == null ||
            Defense == null ||
            Stamina == null;

        #endregion
    }
}