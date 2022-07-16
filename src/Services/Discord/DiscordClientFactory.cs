namespace StatsReporter.Services.Discord
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.Entities;

    using StatsReporter.Commands;
    using StatsReporter.Configuration;
    using StatsReporter.Extensions;

    public class DiscordClientFactory
    {
        public static DiscordClient CreateDiscordClient(DiscordServerConfig config, IServiceProvider services)
        {
            if (string.IsNullOrEmpty(config?.Bot?.Token))
            {
                throw new NullReferenceException("DiscordClient bot token must be set!");
            }

            var client = new DiscordClient(new DiscordConfiguration
            {
                AutoReconnect = true,
                AlwaysCacheMembers = true,
                // REVIEW: Hmm maybe we should compress the whole stream instead of just payload.
                GatewayCompressionLevel = GatewayCompressionLevel.Payload,
                Token = config.Bot?.Token,
                TokenType = TokenType.Bot,
                MinimumLogLevel = config.LogLevel,
                Intents = DiscordIntents.DirectMessages
                    | DiscordIntents.DirectMessageTyping
                    | DiscordIntents.GuildEmojis
                    | DiscordIntents.GuildMembers
                    | DiscordIntents.GuildMessages
                    | DiscordIntents.GuildMessageTyping
                    | DiscordIntents.GuildPresences
                    | DiscordIntents.Guilds
                    | DiscordIntents.GuildWebhooks,
                ReconnectIndefinitely = true,
            });

            // Discord commands configuration
            var commands = client.UseCommandsNext
            (
                new CommandsNextConfiguration
                {
                    StringPrefixes = new[] { config.Bot?.CommandPrefix?.ToString() },
                    EnableDms = true,
                    // If command prefix is null, allow for mention prefix
                    EnableMentionPrefix = string.IsNullOrEmpty(config.Bot?.CommandPrefix),
                    // Use DSharpPlus's built-in help formatter
                    EnableDefaultHelp = true,
                    CaseSensitive = false,
                    IgnoreExtraArguments = true,
                    Services = services,
                }
            );
            // Register available Discord command handler classes
            commands.RegisterCommands<DailyStats>();

            commands.CommandExecuted += Commands_CommandExecuted;
            commands.CommandErrored += Commands_CommandErrored;
            return client;
        }

        private static async Task Commands_CommandExecuted(CommandsNextExtension commands, CommandExecutionEventArgs e)
        {
            // let's log the name of the command and user
            Console.WriteLine($"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            await Task.CompletedTask;
        }

        private static async Task Commands_CommandErrored(CommandsNextExtension commands, CommandErrorEventArgs e)
        {
            Console.WriteLine($"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? e.Context.Message.Content}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            // let's check if the error is a result of lack of required permissions
            if (e.Exception is DSharpPlus.CommandsNext.Exceptions.ChecksFailedException)
            {
                // The user lacks required permissions, 
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                // let's wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync(embed: embed);
            }
            else if (e.Exception is ArgumentException)
            {
                var config = commands.Services.GetService(typeof(Config)) as Config;
                var arguments = e.Command?.Overloads[0];
                // The user lacks required permissions, 
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":x:");

                var guildId = e.Context.Guild?.Id ?? e.Context.Client.Guilds.FirstOrDefault(guild => config?.Servers.ContainsKey(guild.Key) ?? false).Key;
                var prefix = config?.Servers.ContainsKey(guildId) ?? false
                    ? config.Servers[guildId].Bot.CommandPrefix
                    : "!";
                var args = string.Join(" ", arguments.Arguments.Select(arg => arg.IsOptional ? $"[{arg.Name}]" : arg.Name));
                var example = $"Command Example: ```{prefix}{e.Command?.Name} {args}```\r\n*Parameters in brackets are optional.*";

                // let's wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{emoji} Invalid Argument(s)",
                    Description = $"{string.Join(Environment.NewLine, arguments.Arguments.Select(arg => $"Parameter **{arg.Name}** expects type **{arg.Type.ToHumanReadableString()}.**"))}.\r\n\r\n{example}",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync(embed: embed);
            }
            else if (e.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException)
            {
                Console.WriteLine($"User {e.Context.User.Username} tried executing command {e.Context.Message.Content} but command does not exist.");
            }
            else
            {
                Console.WriteLine($"User {e.Context.User.Username} tried executing command {e.Command?.Name} and unknown error occurred.\r\n: {e.Exception}");
            }
        }
    }
}