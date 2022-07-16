using Microsoft.Extensions.Logging;

using PokemonStatsReporter;
using PokemonStatsReporter.Configuration;
using PokemonStatsReporter.Services.Discord;
using PokemonStatsReporter.Services.Localization;
using PokemonStatsReporter.Services.Statistics;

// Load configuration file
var logger = new Logger<Program>(LoggerFactory.Create(x => x.AddConsole()));
var config = Config.Load(Strings.ConfigFilePath);
if (config == null)
{
    logger.LogError($"Failed to load config.json, exiting...");
    Environment.FailFast("Failed to load config.json");
}

// Generate and load translation files
await Translator.CreateLocaleFiles();
Translator.Instance.SetLocale(config.Locale);

// Start Discord clients service
var discordClientService = new DiscordClientService(config);
await discordClientService.Start();

// Start statistic reports service
var reportsService = new StatisticReportsService(config, discordClientService);
reportsService.Start();

logger.LogInformation($"Initialized...");

System.Diagnostics.Process.GetCurrentProcess().WaitForExit();