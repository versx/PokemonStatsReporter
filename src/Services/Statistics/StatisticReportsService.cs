namespace PokemonStatsReporter.Services.Statistics
{
    using DSharpPlus;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    using PokemonStatsReporter.Configuration;
    using PokemonStatsReporter.Data.Entities;
    using PokemonStatsReporter.Data.Factories;
    using PokemonStatsReporter.Extensions;
    using PokemonStatsReporter.Services.Discord;
    using PokemonStatsReporter.Services.Localization;
    using PokemonStatsReporter.Services.Statistics.Models;

    // TODO: Simplified IV stats postings via command with arg `list`
    // TODO: Get total IV found for IV stats
    // TODO: Include forms with shiny/iv stats
    // TODO: Make statistic reporting plugin-able to support unlimited stat reporting

    // TODO: Add custom date format

    public class StatisticReportsService : IStatisticReportsService, IDisposable
    {
        #region Constants

        private const string StatisticDateFormat = "yyyy/MM/dd";
        private const ushort MaxDatabaseTimeoutS = 30;

        #endregion

        #region Variables

        private static readonly ILogger<StatisticReportsService> _logger =
            new Logger<StatisticReportsService>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly Dictionary<string, MidnightTimer> _tzMidnightTimers;
        private readonly Config _config;
        private readonly IDiscordClientService _discordService;

        #endregion

        #region Constructor

        public StatisticReportsService(
            Config config,
            IDiscordClientService discordService)
        {
            _config = config;
            _discordService = discordService;
            _tzMidnightTimers = new Dictionary<string, MidnightTimer>();
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            _logger.LogDebug($"Starting daily statistic reporting service...");

            var localZone = TimeZoneInfo.Local;
            var timezone = localZone.StandardName;

            var midnightTimer = new MidnightTimer(0, timezone);
            midnightTimer.TimeReached += OnMidnightTimerTimeReached;
            midnightTimer.Start();

            _tzMidnightTimers.Add(timezone, midnightTimer);
        }

        public void Stop()
        {
            _logger.LogDebug($"Stopping daily statistic reporting service...");

            foreach (var (_, midnightTimer) in _tzMidnightTimers)
            {
                midnightTimer.Stop();
                midnightTimer.Dispose();
            }
        }

        public void Dispose()
        {
            _logger.LogInformation($"Garbage collector disposing of daily statistic reporting service...");

            _tzMidnightTimers.Clear();

            GC.SuppressFinalize(this);
        }

        public static async Task PostShinyStatsAsync(ulong guildId, Config config, DiscordClient client)
        {
            if (!config.Servers.ContainsKey(guildId))
            {
                // Guild not configured
                _logger.LogError(Translator.Instance.Translate("ERROR_NOT_IN_DISCORD_SERVER"));
                return;
            }

            var server = config.Servers[guildId];
            var statsConfig = server.DailyStats?.ShinyStats;
            if (!(statsConfig?.Enabled ?? false))
            {
                // Shiny statistics reporting not enabled
                _logger.LogDebug($"Skipping shiny stats posting for guild '{guildId}', reporting not enabled.");
                return;
            }

            if (!client.Guilds.ContainsKey(guildId))
            {
                // Discord client not in specified guild
                _logger.LogWarning($"Discord client is not in guild '{guildId}'");
                return;
            }

            var guild = client.Guilds[guildId];
            var channelId = statsConfig?.ChannelId ?? 0;
            if (!guild.Channels.ContainsKey(channelId) || channelId == 0)
            {
                // Discord channel does not exist in guild
                _logger.LogWarning($"Channel with ID '{channelId}' does not exist in guild '{guild.Name}' ({guildId})");
                return;
            }

            var statsChannel = await client.GetChannelAsync(channelId);
            if (statsChannel == null)
            {
                _logger.LogError($"Failed to get channel id {channelId} to post shiny stats, are you sure it exists?");
                return;
            }

            if (statsConfig?.ClearMessages ?? false)
            {
                _logger.LogInformation($"Starting shiny statistics channel message clearing for channel '{channelId}' in guild '{guildId}'...");
                await client.DeleteMessagesAsync(channelId);
            }

            var stats = await GetShinyStatsAsync(config.Database.ToString(), config.StatHours);
            if ((stats?.Count ?? 0) == 0)
            {
                _logger.LogError($"Failed to get shiny stats from database, returned 0 entries.");
                return;
            }

            var sorted = stats.Keys.ToList();
            sorted.Sort();
            if (sorted.Count > 0)
            {
                var date = DateTime.Now.Subtract(TimeSpan.FromHours(24)).ToLongDateString();
                await statsChannel.SendMessageAsync(Translator.Instance.Translate("SHINY_STATS_TITLE").FormatText(new { date }));
                await statsChannel.SendMessageAsync(Translator.Instance.Translate("SHINY_STATS_NEWLINE"));
            }

            foreach (var pokemonId in sorted)
            {
                if (pokemonId == 0)
                    continue;

                var pkmnName = Translator.Instance.GetPokemonName(pokemonId);
                var pkmnStats = stats[pokemonId];
                var chance = pkmnStats.Shiny == 0 || pkmnStats.Total == 0
                    ? 0
                    : Convert.ToInt32(pkmnStats.Total / pkmnStats.Shiny);
                var message = chance == 0
                    ? "SHINY_STATS_MESSAGE"
                    : "SHINY_STATS_MESSAGE_WITH_RATIO";
                await statsChannel.SendMessageAsync(Translator.Instance.Translate(message).FormatText(new
                {
                    pokemon = pkmnName,
                    id = pokemonId,
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
                _logger.LogError(Translator.Instance.Translate("ERROR_NOT_IN_DISCORD_SERVER"));
                return;
            }

            var server = config.Servers[guildId];
            var statsConfig = server.DailyStats?.HundoStats;
            if (!(statsConfig?.Enabled ?? false))
            {
                // Hundo statistics reporting not enabled
                _logger.LogDebug($"Skipping hundo stats posting for guild '{guildId}', reporting not enabled.");
                return;
            }

            if (!client.Guilds.ContainsKey(guildId))
            {
                // Discord client not in specified guild
                _logger.LogWarning($"Discord client is not in guild '{guildId}'");
                return;
            }

            var guild = client.Guilds[guildId];
            var channelId = statsConfig?.ChannelId ?? 0;
            if (!guild.Channels.ContainsKey(channelId) || channelId == 0)
            {
                // Discord channel does not exist in guild
                _logger.LogWarning($"Channel with ID '{channelId}' does not exist in guild '{guild.Name}' ({guildId})");
                return;
            }

            var statsChannel = await client.GetChannelAsync(channelId);
            if (statsChannel == null)
            {
                _logger.LogError($"Failed to get channel id {channelId} to post hundo stats, are you sure it exists?");
                return;
            }

            if (statsConfig?.ClearMessages ?? false)
            {
                _logger.LogInformation($"Starting hundo statistics channel message clearing for channel '{channelId}' in guild '{guildId}'...");
                await client.DeleteMessagesAsync(channelId);
            }

            var stats = await GetHundoStatsAsync(config.Database.ToString(), config.StatHours);
            if ((stats?.Count ?? 0) == 0)
            {
                _logger.LogError($"Failed to get hundo stats from database, returned 0 entries.");
                return;
            }

            var sorted = stats.Keys.ToList();
            sorted.Sort();
            if (sorted.Count > 0)
            {
                var date = DateTime.Now.Subtract(TimeSpan.FromHours(24)).ToLongDateString();
                await statsChannel.SendMessageAsync(Translator.Instance.Translate("HUNDO_STATS_TITLE").FormatText(new { date }));
                await statsChannel.SendMessageAsync(Translator.Instance.Translate("HUNDO_STATS_NEWLINE"));
            }

            foreach (var pokemonId in sorted)
            {
                if (pokemonId == 0)
                    continue;

                var pkmnName = Translator.Instance.GetPokemonName(pokemonId);
                var pkmnStats = stats[pokemonId];
                var chance = pkmnStats.Count == 0 || pkmnStats.Total == 0
                    ? 0
                    : Convert.ToInt32(pkmnStats.Total / pkmnStats.Count);
                var message = chance == 0
                    ? "HUNDO_STATS_MESSAGE"
                    : "HUNDO_STATS_MESSAGE_WITH_RATIO";
                await statsChannel.SendMessageAsync(Translator.Instance.Translate(message).FormatText(new
                {
                    pokemon = pkmnName,
                    id = pokemonId,
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

        public static async Task PostIvStatsAsync(ulong guildId, Config config, DiscordClient client, double minimumIV = 100)
        {
            if (!config.Servers.ContainsKey(guildId))
            {
                // Guild not configured
                _logger.LogError(Translator.Instance.Translate("ERROR_NOT_IN_DISCORD_SERVER"));
                return;
            }

            var server = config.Servers[guildId];
            if (!server.DailyStats.IvStats.Enabled)
            {
                // Custom IV statistics reporting not enabled
                _logger.LogDebug($"Skipping IV stats posting for guild '{guildId}', reporting not enabled.");
                return;
            }

            var statsConfig = server.DailyStats?.IvStats;
            if (!client.Guilds.ContainsKey(guildId))
            {
                // Discord client not in specified guild
                _logger.LogWarning($"Discord client is not in guild '{guildId}'");
                return;
            }

            var guild = client.Guilds[guildId];
            var channelId = statsConfig?.ChannelId ?? 0;
            if (!guild.Channels.ContainsKey(channelId) || channelId == 0)
            {
                // Discord channel does not exist in guild
                _logger.LogWarning($"Channel with ID '{channelId}' does not exist in guild '{guild.Name}' ({guildId})");
                return;
            }

            var statsChannel = await client.GetChannelAsync(channelId);
            if (statsChannel == null)
            {
                _logger.LogError($"Failed to get channel id {channelId} to post IV stats.");
                return;
            }

            if (statsConfig?.ClearMessages ?? false)
            {
                _logger.LogInformation($"Starting IV statistics channel message clearing for channel '{channelId}' in guild '{guildId}'...");
                await client.DeleteMessagesAsync(channelId);
            }

            if (minimumIV == -1)
            {
                // If report triggered via Discord command and no minimum IV specified,
                // use default in stats config. If no minimum IV specified in config
                // use default of 100.
                minimumIV = statsConfig?.MinimumIV ?? 100;
            }

            var stats = GetIvStats(config.Database.ToString(), minimumIV, config.StatHours);
            if ((stats?.Count ?? 0) == 0)
            {
                _logger.LogError($"Failed to get IV stats from database, returned 0 entries.");
                return;
            }

            var sorted = stats.Keys.ToList();
            sorted.Sort();
            if (stats.Count > 0)
            {
                var date = DateTime.Now.Subtract(TimeSpan.FromHours(24)).ToLongDateString();
                await statsChannel.SendMessageAsync(Translator.Instance.Translate("IV_STATS_TITLE").FormatText(new { iv = minimumIV, date }));
                await statsChannel.SendMessageAsync(Translator.Instance.Translate("IV_STATS_NEWLINE"));
            }

            //foreach (var (pokemonId, count) in stats)
            foreach (var pokemonId in sorted)
            {
                if (pokemonId == 0)
                    continue;

                //var count = stats[pokemonId];
                //var total = 0; // TODO: Total IV stats
                //var ratio = 0; // TODO: Ratio IV stats
                var pkmnStats = new { Count = 0, Total = 0 }; // stats[pokemonId];
                var pkmnName = Translator.Instance.GetPokemonName(pokemonId);
                //sb.AppendLine($"- {pkmn.Name} (#{key}) {count:N0}");

                //var chance = pkmnStats.Count == 0 || pkmnStats.Total == 0 ? 0 : Convert.ToInt32(pkmnStats.Total / pkmnStats.Count);
                var chance = 0;
                var message = chance == 0
                    ? "IV_STATS_MESSAGE"
                    : "IV_STATS_MESSAGE_WITH_RATIO";
                await statsChannel.SendMessageAsync(Translator.Instance.Translate(message).FormatText(new
                {
                    pokemon = pkmnName,
                    id = pokemonId,
                    count = pkmnStats.Count.ToString("N0"),
                    total = pkmnStats.Total.ToString("N0"),
                    chance,
                    iv = minimumIV,
                }));
            }

            await statsChannel.SendMessageAsync(Translator.Instance.Translate("IV_STATS_TOTAL_MESSAGE_WITH_RATIO").FormatText(new
            {
                count = 100,
                total = 1000,
                chance = 1,
                iv = minimumIV,
                // TODO: count = total.Count.ToString("N0"),
                // TODO: total = total.Total.ToString("N0"),
                // TODO: chance = totalRatio,
            }));
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

        #region Private Methods

        private async void OnMidnightTimerTimeReached(DateTime time, string timezone)
        {
            _logger.LogInformation($"Midnight timer triggered, starting statistics reporting...");

            foreach (var (guildId, guildConfig) in _config.Servers)
            {
                if (!_discordService.DiscordClients.ContainsKey(guildId))
                {
                    continue;
                }

                var client = _discordService.DiscordClients[guildId];
                var dailyStatsConfig = guildConfig.DailyStats;
                if (dailyStatsConfig?.ShinyStats?.Enabled ?? false)
                {
                    _logger.LogInformation($"Starting daily shiny stats posting for guild '{guildId}'...");
                    await PostShinyStatsAsync(guildId, _config, client);
                    _logger.LogInformation($"Finished daily shiny stats posting for guild '{guildId}'.");
                }

                if (dailyStatsConfig?.HundoStats?.Enabled ?? false)
                {
                    _logger.LogInformation($"Starting daily hundo stats posting for guild '{guildId}'...");
                    await PostHundoStatsAsync(guildId, _config, client);
                    _logger.LogInformation($"Finished daily hundo stats posting for guild '{guildId}'.");
                }

                if (dailyStatsConfig?.IvStats?.Enabled ?? false)
                {
                    _logger.LogInformation($"Starting daily IV stats posting for guild '{guildId}'...");
                    await PostIvStatsAsync(guildId, _config, client);
                    _logger.LogInformation($"Finished daily IV stats posting for guild '{guildId}'.");
                }

                _logger.LogInformation($"Finished daily stats posting for guild '{guildId}'...");
            }

            _logger.LogInformation($"Finished daily stats reporting for all guilds.");
        }

        private static async Task<Dictionary<uint, ShinyPokemonStats>> GetShinyStatsAsync(string connectionString, ushort statHours = 24)
        {
            var list = new Dictionary<uint, ShinyPokemonStats>
            {
                { 0, new ShinyPokemonStats { PokemonId = 0 } }
            };
            try
            {
                using var ctx = DbContextFactory.CreateMapContext(connectionString);
                ctx.Database.SetCommandTimeout(MaxDatabaseTimeoutS); // 30 seconds timeout
                var yesterday = DateTime.Now.Subtract(TimeSpan.FromHours(statHours)).ToString(StatisticDateFormat);
                var pokemonShiny = (await ctx.PokemonStatsShiny.ToListAsync())
                    .Where(stat => stat.Date.ToString(StatisticDateFormat) == yesterday)
                    .ToList();
                var pokemonIV = (await ctx.PokemonStatsIV.ToListAsync())
                    .Where(stat => stat.Date.ToString(StatisticDateFormat) == yesterday)?
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
                        list[curPkmn.PokemonId].Total += (pokemonIV?.ContainsKey(curPkmn.PokemonId) ?? false)
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
                _logger.LogError($"GetShinyStatsAsync: {ex}");
            }
            return list;
        }

        private static async Task<Dictionary<uint, HundoPokemonStats>> GetHundoStatsAsync(string connectionString, ushort statHours = 24)
        {
            var list = new Dictionary<uint, HundoPokemonStats>
            {
                { 0, new HundoPokemonStats { PokemonId = 0 } }
            };
            try
            {
                using var ctx = DbContextFactory.CreateMapContext(connectionString);
                ctx.Database.SetCommandTimeout(MaxDatabaseTimeoutS);
                var yesterday = DateTime.Now.Subtract(TimeSpan.FromHours(statHours)).ToString(StatisticDateFormat);
                var pokemonHundo = (await ctx.PokemonStatsHundo.ToListAsync())
                    .Where(stat => stat.Date.ToString(StatisticDateFormat) == yesterday)
                    .ToList();
                var pokemonIV = (await ctx.PokemonStatsIV.ToListAsync())
                    .Where(stat => stat.Date.ToString(StatisticDateFormat) == yesterday)?
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
                        list[curPkmn.PokemonId].Total += (pokemonIV?.ContainsKey(curPkmn.PokemonId) ?? false)
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
                _logger.LogError($"GetHundoStatsAsync: {ex}");
            }
            return list;
        }

        private static Dictionary<uint, Dictionary<double, ulong>> GetIvStats(string connectionString, double minimumIV, ushort statHours = 24)
        {
            // TODO: Get IV stats with and without IV Pokemon { 25: { total, count, date }, etc.. }
            try
            {
                using var ctx = DbContextFactory.CreateMapContext(connectionString);
                ctx.Database.SetCommandTimeout(MaxDatabaseTimeoutS);
                var now = DateTime.Now;
                var hoursAgo = TimeSpan.FromHours(statHours);

                var test = now.Subtract(hoursAgo);
                var yesterday = Convert.ToUInt64(Math.Round(test.GetUnixTimestamp()));

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                var pokemon = ctx.Pokemon.Where(p => (ulong)p.DisappearTime < yesterday)
                                         .ToList();

                // Total counts of scanned Pokemon with and without IV
                var pokemonWithoutIV = pokemon.Where(p => p.IsMissingStats)
                                              .GroupBy(x => x.PokemonId) //y => y
                                              .ToDictionary(x => x.Key, y => y.Count());
                var pokemonWithIV = pokemon.Where(p => !p.IsMissingStats)
                                           .ToList();
                                           //.Where(p => p.IVReal >= minimumIV)
                                           //.Where(p => p.Attack >= 13 && p.Defense >= 13 && p.Stamina >= 13)
                                           //.GroupBy(x => x.PokemonId)
                                           //.ToDictionary(x => x.Key, y => y.Count());

                sw.Stop();
                var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
                _logger.LogDebug($"Took: {totalSeconds}");

                // Final dictionary
                //var filteredPokemonWithIV = pokemonWithIV.Where(p => p.IVReal >= minimumIV)
                //                                         .ToList();


                // Checks within last 24 hours and specified IV (or use statistics cache?)

                /*
                var count = ctx.Pokemon.Where(p => p.DisappearTime < yesterday)
                                       .Where(p => p.Attack != null && p.Defense != null && p.Stamina != null)
                                       .ToList()
                                       .GroupBy(x => x.PokemonId, y => y)
                                       .Select(x => new { id = x.Key, count = x.Count() })
                                       .ToList();
                */

                /*
                var pokemon2 = ctx.Pokemon
                    .Where(p => p.DisappearTime < yesterday)
                    .Where(p => p.Attack != null && p.Defense != null && p.Stamina != null)
                    //.AsEnumerable()
                    .ToList()
                    .Where(p => p.IVReal >= minimumIV)
                    .ToList();
                */
                    //.Where(p => !p.IsMissingStats)
                    //.Where(p => p.IVReal >= minimumIV)
                    //.ToList();
                    //.GroupBy(x => x.PokemonId, y => y.IVReal)
                    ////.GroupBy(x => x.Key, y => y.Count())
                    ////.ToDictionary(x => x.Key, y => y.Count());
                    //.ToDictionary(x => x.Key, y => y);

                // pokemonId: { iv: count } or pokemonId: [{ iv, count }]
                var manifest = BuildIvStatsManifest(pokemonWithIV, 90);
                return manifest;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetIvStats: {ex}");
            }
            return null;
        }

        private static Dictionary<uint, Dictionary<double, ulong>> BuildIvStatsManifest(List<Pokemon> pokemon, double minimumIV = 100)
        {
            // TODO: Replace method with Linq equivilant eventually
            var dict = new Dictionary<uint, Dictionary<double, ulong>>();
            foreach (var pkmn in pokemon)
            {
                if (pkmn.IVReal < minimumIV)
                    continue;

                if (!dict.ContainsKey(pkmn.PokemonId))
                {
                    dict.Add(pkmn.PokemonId, new());
                }
                if (!dict[pkmn.PokemonId].ContainsKey(pkmn.IVReal))
                {
                    dict[pkmn.PokemonId].Add(pkmn.IVReal, 0);
                }
                dict[pkmn.PokemonId][pkmn.IVReal]++;
            }
            _logger.LogError($"BuildIvStatsManifest: {dict}");
            return dict;
        }

        #endregion
    }
}