namespace StatsReporter.Services.Discord
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using StatsReporter.Configuration;

    public class DiscordClientService : IDiscordClientService
    {
        private readonly ILogger<IDiscordClientService> _logger;
        private readonly Dictionary<ulong, DiscordClient> _discordClients;
        private readonly Config _config;

        public IReadOnlyDictionary<ulong, DiscordClient> DiscordClients =>
            _discordClients;

        public bool Initialized { get; private set; }

        public DiscordClientService(Config config)
        {
            _config = config;
            _logger = new Logger<IDiscordClientService>(LoggerFactory.Create(x => x.AddConsole()));
            _discordClients = new Dictionary<ulong, DiscordClient>();
        }

        #region Public Methods

        public async Task Start()
        {
            _logger.LogTrace($"Initializing Discord clients...");

            // Build the dependency collection which will contain our objects that can be
            // globally used within each command module
            var servicesCol = new ServiceCollection()
                .AddSingleton(typeof(Config), _config)
                .AddSingleton(LoggerFactory.Create(configure => configure.AddConsole()));
            var services = servicesCol.BuildServiceProvider();

            await InitializeDiscordClients(services);
        }

        public async Task Stop()
        {
            _logger.LogTrace($"Stopping Discord clients...");

            foreach (var (guildId, discordClient) in _discordClients)
            {
                await discordClient.DisconnectAsync();
                _logger.LogDebug($"Discord client for guild {guildId} disconnected.");
            }
        }

        #endregion

        private async Task InitializeDiscordClients(ServiceProvider services)
        {
            foreach (var (guildId, guildConfig) in _config.Servers)
            {
                _logger.LogDebug($"Configured Discord server {guildId}");
                var client = DiscordClientFactory.CreateDiscordClient(guildConfig, services);
                client.Ready += Client_Ready;
                client.GuildAvailable += Client_GuildAvailable;
                //client.MessageCreated += Client_MessageCreated;
                client.ClientErrored += Client_ClientErrored;

                if (!_discordClients.ContainsKey(guildId))
                {
                    _discordClients.Add(guildId, client);
                    await client.ConnectAsync();
                    _logger.LogDebug($"Discord client for guild {guildId} connecting...");
                }

                // Wait 3 seconds between initializing each Discord client
                await Task.Delay(3 * 1000);
            }

            _logger.LogInformation($"Discord clients all initialized");
            Initialized = true;
        }

        #region Discord Events

        private Task Client_Ready(DiscordClient client, ReadyEventArgs e)
        {
            _logger.LogInformation($"------------------------------------------");
            _logger.LogInformation($"[DISCORD] Connected.");
            _logger.LogInformation($"[DISCORD] ----- Current Application");
            _logger.LogInformation($"[DISCORD] Name: {client.CurrentApplication.Name}");
            _logger.LogInformation($"[DISCORD] Description: {client.CurrentApplication.Description}");
            var owners = string.Join(", ", client.CurrentApplication.Owners.Select(owner => $"{owner.Username}#{owner.Discriminator}"));
            _logger.LogInformation($"[DISCORD] Owner: {owners}");
            _logger.LogInformation($"[DISCORD] ----- Current User");
            _logger.LogInformation($"[DISCORD] Id: {client.CurrentUser.Id}");
            _logger.LogInformation($"[DISCORD] Name: {client.CurrentUser.Username}#{client.CurrentUser.Discriminator}");
            _logger.LogInformation($"[DISCORD] Email: {client.CurrentUser.Email}");
            _logger.LogInformation($"------------------------------------------");

            return Task.CompletedTask;
        }

        private async Task Client_GuildAvailable(DiscordClient client, GuildCreateEventArgs e)
        {
            // If guild is in configured servers list then attempt to create emojis needed
            if (!_config.Servers.ContainsKey(e.Guild.Id))
                return;

            // Set custom bot status if guild is in config server list, otherwise set to bot version by default
            var status = _config.Servers[e.Guild.Id].Bot?.Status;
            var botStatus = string.IsNullOrEmpty(status)
                ? $"v{Strings.BotVersion}"
                : status;
            await client.UpdateStatusAsync(new DiscordActivity(botStatus, ActivityType.Playing), UserStatus.Online);
        }

        private async Task Client_ClientErrored(DiscordClient client, ClientErrorEventArgs e)
        {
            _logger.LogError(e.Exception.ToString());

            await Task.CompletedTask;
        }

        #endregion
    }
}