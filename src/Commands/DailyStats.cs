namespace PokemonStatsReporter.Commands
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;

    using PokemonStatsReporter.Configuration;
    using PokemonStatsReporter.Services.Statistics;

    [RequirePermissions(Permissions.KickMembers)]
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
        public async Task GetIVStatsAsync(CommandContext ctx, double minimumIV = -1)
        {
            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(guildId => _config.Servers.ContainsKey(guildId));
            if (guildId > 0)
            {
                await StatisticReportsService.PostIvStatsAsync(guildId, _config, ctx.Client, minimumIV);
            }
        }

        #endregion
    }
}