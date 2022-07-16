namespace PokemonStatsReporter.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

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
        public double IVReal
        {
            get
            {
                if (Attack == null || Defense == null || Stamina == null)
                {
                    return -1;
                }

                var atk = Attack ?? 0;
                var def = Defense ?? 0;
                var sta = Stamina ?? 0;

                return Math.Round((sta + atk + def) * 100.0 / 45.0, 1);
            }
        }

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
            JsonPropertyName("display_pokemon_id"),
            Column("display_pokemon_id"),
        ]
        public uint? DisplayPokemonId { get; set; }

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