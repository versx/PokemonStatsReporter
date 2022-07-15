namespace StatsReporter.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json.Serialization;

    using StatsReporter.Extensions;

    public class GameMaster
    {
        public const string MasterFileName = "masterfile.json";

        #region Properties

        [JsonPropertyName("pokemon")]
        public IReadOnlyDictionary<uint, PokedexPokemon> Pokedex { get; set; }

        #region Singletons

        private static GameMaster _instance;
        public static GameMaster Instance
        {
            get
            {
                if (_instance == null)
                {
                    ReloadMasterFile();
                }
                return _instance;
            }
        }

        #endregion

        #endregion

        public static PokedexPokemon GetPokemon(uint pokemonId, uint formId = 0)
        {
            if (pokemonId == 0)
                return null;

            if (!Instance.Pokedex.ContainsKey(pokemonId))
            {
                Console.WriteLine($"[Warning] Pokemon {pokemonId} does not exist in {MasterFileName}, please use an updated version.");
                return null;
            }

            var pkmn = Instance.Pokedex[pokemonId];
            var useForm = /*!pkmn.Attack.HasValue &&*/ formId > 0 && pkmn.Forms.ContainsKey(formId);
            var pkmnForm = useForm ? pkmn.Forms[formId] : pkmn;
            pkmnForm.Name = pkmn.Name;
            // Check if Pokemon is form and Pokemon types provided, if not use normal Pokemon types as fallback
            pkmnForm.Types = useForm && (pkmn.Forms[formId].Types?.Count ?? 0) > 0
                ? pkmn.Forms[formId].Types
                : pkmn.Types;
            return pkmnForm;
        }

        public static void ReloadMasterFile()
        {
            var path = Path.Combine(Strings.DataFolder, MasterFileName);
            _instance = path.LoadFromFile<GameMaster>();
        }
    }
}