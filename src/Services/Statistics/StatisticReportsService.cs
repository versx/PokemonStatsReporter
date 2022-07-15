namespace StatsReporter.Services.Statistics
{
    using DSharpPlus;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    using StatsReporter.Configuration;
    using StatsReporter.Data;
    using StatsReporter.Data.Factories;
    using StatsReporter.Extensions;
    using StatsReporter.Services.Discord;
    using StatsReporter.Services.Localization;
    using StatsReporter.Services.Statistics.Models;

    public class StatisticReportsService : IStatisticReportsService, IDisposable
    {
        #region Variables

        private readonly ILogger<StatisticReportsService> _logger;
        private readonly Dictionary<string, MidnightTimer> _tzMidnightTimers;
        private readonly Config _config;
        private readonly IDiscordClientService _discordService;

        #endregion

        #region Constructor

        public StatisticReportsService(
            Config config,
            IDiscordClientService discordService)
        {
            _logger = new Logger<StatisticReportsService>(LoggerFactory.Create(x => x.AddConsole())); ;
            _config = config;
            _discordService = discordService;
            _tzMidnightTimers = new Dictionary<string, MidnightTimer>();
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            _logger.LogDebug($"Starting daily statistic reporting hosted service...");

            var localZone = TimeZoneInfo.Local;
            var timezone = localZone.StandardName;

            var midnightTimer = new MidnightTimer(0, timezone);
            midnightTimer.TimeReached += OnMidnightTimerTimeReached;
            midnightTimer.Start();

            _tzMidnightTimers.Add(timezone, midnightTimer);
        }

        public void Stop()
        {
            _logger.LogDebug($"Stopping daily statistic reporting hosted service...");

            foreach (var (_, midnightTimer) in _tzMidnightTimers)
            {
                midnightTimer.Stop();
                midnightTimer.Dispose();
            }
        }

        public void Dispose()
        {
            _tzMidnightTimers.Clear();

            GC.SuppressFinalize(this);
        }

        #endregion

        private async void OnMidnightTimerTimeReached(DateTime time, string timezone)
        {
            foreach (var (guildId, guildConfig) in _config.Servers)
            {
                if (!_discordService.DiscordClients.ContainsKey(guildId))
                {
                    continue;
                }

                var client = _discordService.DiscordClients[guildId];
                if (guildConfig.DailyStats?.ShinyStats?.Enabled ?? false)
                {
                    _logger.LogInformation($"Starting daily shiny stats posting for guild '{guildId}'...");
                    await PostShinyStatsAsync(guildId, _config, client);
                    _logger.LogInformation($"Finished daily shiny stats posting for guild '{guildId}'.");
                }

                if (guildConfig.DailyStats?.IVStats?.Enabled ?? false)
                {
                    _logger.LogInformation($"Starting daily hundo stats posting for guild '{guildId}'...");
                    await PostHundoStatsAsync(guildId, _config, client);
                    _logger.LogInformation($"Finished daily hundo stats posting for guild '{guildId}'.");
                }

                _logger.LogInformation($"Finished daily stats posting for guild '{guildId}'...");
            }

            _logger.LogInformation($"Finished daily stats reporting for all guilds.");
        }

        public static async Task PostShinyStatsAsync(ulong guildId, Config config, DiscordClient client)
        {
            if (!config.Servers.ContainsKey(guildId))
            {
                // Guild not configured
                Console.WriteLine(Translator.Instance.Translate("ERROR_NOT_IN_DISCORD_SERVER"));
                return;
            }

            var server = config.Servers[guildId];
            if (!(server.DailyStats?.ShinyStats?.Enabled ?? false))
            {
                // Shiny statistics reporting not enabled
                Console.WriteLine($"Skipping shiny stats posting for guild '{guildId}', reporting not enabled.");
                return;
            }

            if (!client.Guilds.ContainsKey(guildId))
            {
                // Discord client not in specified guild
                Console.WriteLine($"Discord client is not in guild '{guildId}'");
                return;
            }

            var guild = client.Guilds[guildId];
            var channelId = server.DailyStats.ShinyStats.ChannelId;
            if (!guild.Channels.ContainsKey(channelId))
            {
                // Discord channel does not exist in guild
                Console.WriteLine($"Channel with ID '{channelId}' does not exist in guild '{guild.Name}' ({guildId})");
                return;
            }

            var statsChannel = await client.GetChannelAsync(channelId);
            if (statsChannel == null)
            {
                Console.WriteLine($"Failed to get channel id {channelId} to post shiny stats, are you sure it exists?");
                return;
            }

            if (server.DailyStats.ShinyStats.ClearMessages)
            {
                Console.WriteLine($"Starting shiny statistics channel message clearing for channel '{channelId}' in guild '{guildId}'...");
                await client.DeleteMessagesAsync(channelId);
            }

            var stats = await GetShinyStatsAsync(config.Database.ToString());
            var sorted = stats.Keys.ToList();
            sorted.Sort();
            if (sorted.Count > 0)
            {
                var date = DateTime.Now.Subtract(TimeSpan.FromHours(24)).ToLongDateString();
                await statsChannel.SendMessageAsync(Translator.Instance.Translate("SHINY_STATS_TITLE").FormatText(new { date }));
                await statsChannel.SendMessageAsync(Translator.Instance.Translate("SHINY_STATS_NEWLINE"));
            }

            foreach (var pokemon in sorted)
            {
                if (pokemon == 0)
                    continue;

                if (!GameMaster.Instance.Pokedex.ContainsKey(pokemon))
                    continue;

                var pkmn = GameMaster.Instance.Pokedex[pokemon];
                var pkmnStats = stats[pokemon];
                var chance = pkmnStats.Shiny == 0 || pkmnStats.Total == 0 ? 0 : Convert.ToInt32(pkmnStats.Total / pkmnStats.Shiny);
                var message = chance == 0 ? "SHINY_STATS_MESSAGE" : "SHINY_STATS_MESSAGE_WITH_RATIO";
                await statsChannel.SendMessageAsync(Translator.Instance.Translate(message).FormatText(new
                {
                    pokemon = pkmn.Name,
                    id = pokemon,
                    shiny = pkmnStats.Shiny.ToString("N0"),
                    total = pkmnStats.Total.ToString("N0"),
                    chance,
                }));
                Thread.Sleep(500);
            }

            var total = stats[0];
            var totalRatio = total.Shiny == 0 || total.Total == 0
                ? 0
                : Convert.ToInt32(total.Total / total.Shiny);

            await statsChannel.SendMessageAsync(Translator.Instance.Translate("SHINY_STATS_TOTAL_MESSAGE_WITH_RATIO").FormatText(new
            {
                shiny = total.Shiny.ToString("N0"),
                total = total.Total.ToString("N0"),
                chance = totalRatio,
            }));
        }

        public static async Task PostHundoStatsAsync(ulong guildId, Config config, DiscordClient client)
        {
            if (!config.Servers.ContainsKey(guildId))
            {
                // Guild not configured
                Console.WriteLine(Translator.Instance.Translate("ERROR_NOT_IN_DISCORD_SERVER"));
                return;
            }

            var server = config.Servers[guildId];
            if (!(server.DailyStats?.IVStats?.Enabled ?? false))
            {
                // Hundo statistics reporting not enabled
                Console.WriteLine($"Skipping hundo stats posting for guild '{guildId}', reporting not enabled.");
                return;
            }

            if (!client.Guilds.ContainsKey(guildId))
            {
                // Discord client not in specified guild
                Console.WriteLine($"Discord client is not in guild '{guildId}'");
                return;
            }

            var guild = client.Guilds[guildId];
            var channelId = server.DailyStats.IVStats.ChannelId;
            if (!guild.Channels.ContainsKey(channelId))
            {
                // Discord channel does not exist in guild
                Console.WriteLine($"Channel with ID '{channelId}' does not exist in guild '{guild.Name}' ({guildId})");
                return;
            }

            var statsChannel = await client.GetChannelAsync(channelId);
            if (statsChannel == null)
            {
                Console.WriteLine($"Failed to get channel id {channelId} to post hundo stats, are you sure it exists?");
                return;
            }

            if (server.DailyStats.IVStats.ClearMessages)
            {
                Console.WriteLine($"Starting hundo statistics channel message clearing for channel '{channelId}' in guild '{guildId}'...");
                await client.DeleteMessagesAsync(channelId);
            }

            var stats = await GetHundoStatsAsync(config.Database.ToString());
            var sorted = stats.Keys.ToList();
            sorted.Sort();
            if (sorted.Count > 0)
            {
                var date = DateTime.Now.Subtract(TimeSpan.FromHours(24)).ToLongDateString();
                await statsChannel.SendMessageAsync(Translator.Instance.Translate("HUNDO_STATS_TITLE").FormatText(new { date }));
                await statsChannel.SendMessageAsync(Translator.Instance.Translate("HUNDO_STATS_NEWLINE"));
            }

            foreach (var pokemon in sorted)
            {
                if (pokemon == 0)
                    continue;

                if (!GameMaster.Instance.Pokedex.ContainsKey(pokemon))
                    continue;

                var pkmn = GameMaster.Instance.Pokedex[pokemon];
                var pkmnStats = stats[pokemon];
                var chance = pkmnStats.Count == 0 || pkmnStats.Total == 0 ? 0 : Convert.ToInt32(pkmnStats.Total / pkmnStats.Count);
                var message = chance == 0 ? "HUNDO_STATS_MESSAGE" : "HUNDO_STATS_MESSAGE_WITH_RATIO";
                await statsChannel.SendMessageAsync(Translator.Instance.Translate(message).FormatText(new
                {
                    pokemon = pkmn.Name,
                    id = pokemon,
                    count = pkmnStats.Count.ToString("N0"),
                    total = pkmnStats.Total.ToString("N0"),
                    chance,
                }));
                Thread.Sleep(500);
            }

            var total = stats[0];
            var totalRatio = total.Count == 0 || total.Total == 0
                ? 0
                : Convert.ToInt32(total.Total / total.Count);

            await statsChannel.SendMessageAsync(Translator.Instance.Translate("HUNDO_STATS_TOTAL_MESSAGE_WITH_RATIO").FormatText(new
            {
                count = total.Count.ToString("N0"),
                total = total.Total.ToString("N0"),
                chance = totalRatio,
            }));
        }

        internal static async Task<Dictionary<uint, ShinyPokemonStats>> GetShinyStatsAsync(string scannerConnectionString)
        {
            var list = new Dictionary<uint, ShinyPokemonStats>
            {
                { 0, new ShinyPokemonStats { PokemonId = 0 } }
            };
            try
            {
                using var ctx = DbContextFactory.CreateMapContext(scannerConnectionString);
                ctx.Database.SetCommandTimeout(TimeSpan.FromSeconds(30)); // 30 seconds timeout
                var yesterday = DateTime.Now.Subtract(TimeSpan.FromHours(24)).ToString("yyyy/MM/dd");
                var pokemonShiny = (await ctx.PokemonStatsShiny.ToListAsync())
                    .Where(stat => stat.Date.ToString("yyyy/MM/dd") == yesterday)
                    .ToList();
                var pokemonIV = (await ctx.PokemonStatsIV.ToListAsync())
                    .Where(stat => stat.Date.ToString("yyyy/MM/dd") == yesterday)?
                    .ToDictionary(stat => stat.PokemonId);
                for (var i = 0; i < pokemonShiny.Count; i++)
                {
                    var curPkmn = pokemonShiny[i];
                    if (curPkmn.PokemonId > 0)
                    {
                        if (!list.ContainsKey(curPkmn.PokemonId))
                        {
                            list.Add(curPkmn.PokemonId, new ShinyPokemonStats { PokemonId = curPkmn.PokemonId });
                        }

                        list[curPkmn.PokemonId].PokemonId = curPkmn.PokemonId;
                        list[curPkmn.PokemonId].Shiny += Convert.ToUInt64(curPkmn.Count);
                        list[curPkmn.PokemonId].Total += pokemonIV.ContainsKey(curPkmn.PokemonId)
                            ? Convert.ToUInt64(pokemonIV[curPkmn.PokemonId].Count)
                            : 0;
                    }
                }
                list.Values.ToList().ForEach(stat =>
                {
                    list[0].Shiny += stat.Shiny;
                    list[0].Total += stat.Total;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
            return list;
        }

        internal static async Task<Dictionary<uint, HundoPokemonStats>> GetHundoStatsAsync(string scannerConnectionString)
        {
            var list = new Dictionary<uint, HundoPokemonStats>
            {
                { 0, new HundoPokemonStats { PokemonId = 0 } }
            };
            try
            {
                using var ctx = DbContextFactory.CreateMapContext(scannerConnectionString);
                ctx.Database.SetCommandTimeout(TimeSpan.FromSeconds(30)); // 30 seconds timeout
                var yesterday = DateTime.Now.Subtract(TimeSpan.FromHours(24)).ToString("yyyy/MM/dd");
                var pokemonHundo = (await ctx.PokemonStatsHundo.ToListAsync())
                    .Where(stat => stat.Date.ToString("yyyy/MM/dd") == yesterday)
                    .ToList();
                var pokemonIV = (await ctx.PokemonStatsIV.ToListAsync())
                    .Where(stat => stat.Date.ToString("yyyy/MM/dd") == yesterday)?
                    .ToDictionary(stat => stat.PokemonId);
                for (var i = 0; i < pokemonHundo.Count; i++)
                {
                    var curPkmn = pokemonHundo[i];
                    if (curPkmn.PokemonId > 0)
                    {
                        if (!list.ContainsKey(curPkmn.PokemonId))
                        {
                            list.Add(curPkmn.PokemonId, new HundoPokemonStats { PokemonId = curPkmn.PokemonId });
                        }

                        list[curPkmn.PokemonId].PokemonId = curPkmn.PokemonId;
                        list[curPkmn.PokemonId].Count += Convert.ToUInt64(curPkmn.Count);
                        list[curPkmn.PokemonId].Total += pokemonIV.ContainsKey(curPkmn.PokemonId)
                            ? Convert.ToUInt64(pokemonIV[curPkmn.PokemonId].Count)
                            : 0;
                    }
                }
                list.Values.ToList().ForEach(stat =>
                {
                    list[0].Count += stat.Count;
                    list[0].Total += stat.Total;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
            return list;
        }
    }
}