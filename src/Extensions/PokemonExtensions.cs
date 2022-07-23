namespace PokemonStatsReporter.Extensions
{
    using PokemonStatsReporter.Data.Entities;

    public static class PokemonExtensions
    {
        public static double CalculateIV(this Pokemon pkmn)
        {
            if (pkmn.Attack == null || pkmn.Defense == null || pkmn.Stamina == null)
            {
                return -1;
            }

            var atk = pkmn.Attack ?? 0;
            var def = pkmn.Defense ?? 0;
            var sta = pkmn.Stamina ?? 0;

            return Math.Round((sta + atk + def) * 100.0 / 45.0, 1);
        }
    }
}