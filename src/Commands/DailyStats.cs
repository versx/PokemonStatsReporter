namespace StatsReporter.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;
    using Microsoft.EntityFrameworkCore;

    using StatsReporter.Configuration;
    using StatsReporter.Data;
    using StatsReporter.Data.Factories;
    using StatsReporter.Extensions;
    using StatsReporter.Services.Localization;
    using StatsReporter.Services.Statistics;

    // TODO: Simplified IV stats postings via command with arg `list`
    // TODO: Get total IV found for IV stats
    // TODO: Include forms with shiny/iv stats

    public class DailyStats : BaseCommandModule
    {
        private readonly Config _config;

        public DailyStats(Config config)
        {
            _config = config;
        }

        #region Stat Commands

        [
            Command("shiny-stats"),
            RequirePermissions(Permissions.KickMembers),
        ]
        public async Task GetShinyStatsAsync(CommandContext ctx)
        {
            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(guildId => _config.Servers.ContainsKey(guildId));
            if (guildId > 0)
            {
                await StatisticReportsService.PostShinyStatsAsync(guildId, _config, ctx.Client);
            }
        }

        [
            Command("hundo-stats"),
            RequirePermissions(Permissions.KickMembers),
        ]
        public async Task GetHundoStatsAsync(CommandContext ctx)
        {
            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(guildId => _config.Servers.ContainsKey(guildId));
            if (guildId > 0)
            {
                await StatisticReportsService.PostHundoStatsAsync(guildId, _config, ctx.Client);
            }
        }

        [
            Command("iv-stats"),
            RequirePermissions(Permissions.KickMembers),
        ]
        public async Task GetIVStatsAsync(CommandContext ctx, uint minimumIV = 100)
        {
            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(guildId => _config.Servers.ContainsKey(guildId));

            if (!_config.Servers.ContainsKey(guildId))
            {
                await ctx.RespondEmbedAsync(Translator.Instance.Translate("ERROR_NOT_IN_DISCORD_SERVER"), DiscordColor.Red);
                return;
            }

            var server = _config.Servers[guildId];
            if (!server.DailyStats.IVStats.Enabled)
                return;

            var statsChannel = await ctx.Client.GetChannelAsync(server.DailyStats.IVStats.ChannelId);
            if (statsChannel == null)
            {
                Console.WriteLine($"Failed to get channel id {server.DailyStats.IVStats.ChannelId} to post shiny stats.");
                await ctx.RespondEmbedAsync(Translator.Instance.Translate("SHINY_STATS_INVALID_CHANNEL").FormatText(ctx.User.Username), DiscordColor.Yellow);
                return;
            }

            if (server.DailyStats.IVStats.ClearMessages)
            {
                await ctx.Client.DeleteMessagesAsync(server.DailyStats.IVStats.ChannelId);
            }

            var stats = GetIvStats(_config.Database.ToString(), minimumIV);

            var date = DateTime.Now.Subtract(TimeSpan.FromHours(24)).ToLongDateString();
            // TODO: Localize IV stats
            await statsChannel.SendMessageAsync($"[**{minimumIV}% IV Pokemon stats for {date}**]");
            await statsChannel.SendMessageAsync("----------------------------------------------");

            //var sb = new System.Text.StringBuilder();
            var keys = stats.Keys.ToList();
            keys.Sort();
            //foreach (var (pokemonId, count) in stats)
            foreach (var key in keys)
            {
                var count = stats[key];
                var total = 0;
                var ratio = 0;
                var pkmn = GameMaster.GetPokemon(key);
                //sb.AppendLine($"- {pkmn.Name} (#{key}) {count:N0}");
                await statsChannel.SendMessageAsync($"**{pkmn.Name} (#{key})**    |    **{count:N0}** out of **{total}** total seen in the last 24 hours with a **1/{ratio}** ratio.");
            }

            await statsChannel.SendMessageAsync($"Found **8,094** total {minimumIV}% IV Pokemon out of **4,050,641** possiblities with a **1/500** ratio in total.");
            /*
            var embed = new DiscordEmbedBuilder
            {
                Title = $"100% Pokemon Found (Last 24 Hours)",
                Description = sb.ToString(),
            };
            await ctx.RespondAsync(embed.Build());
            */
        }

        #endregion

        internal static Dictionary<uint, int> GetIvStats(string scannerConnectionString, double minIV)
        {
            try
            {
                using var ctx = DbContextFactory.CreateMapContext(scannerConnectionString);
                ctx.Database.SetCommandTimeout(TimeSpan.FromSeconds(30)); // 30 seconds timeout
                var now = DateTime.UtcNow;
                var hoursAgo = TimeSpan.FromHours(24);
                var yesterday = Convert.ToInt64(Math.Round(now.Subtract(hoursAgo).GetUnixTimestamp()));
                // Checks within last 24 hours and 100% IV (or use statistics cache?)
                var pokemon = ctx.Pokemon
                    .AsEnumerable()
                    .Where(pokemon => pokemon.Attack != null && pokemon.Defense != null && pokemon.Stamina != null
                        && pokemon.DisappearTime > yesterday
                        && pokemon.IVReal >= minIV
                      //&& x.Attack == 15
                      //&& x.Defense == 15
                      //&& x.Stamina == 15
                      )
                    .AsEnumerable()
                    .GroupBy(x => x.PokemonId, y => y.IV)
                    .Select(g => new { name = g.Key, count = g.Count() })
                    .ToDictionary(x => x.name, y => y.count);
                return pokemon;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
            return null;
        }
    }
}