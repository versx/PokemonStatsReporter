namespace PokemonStatsReporter.Services.Discord
{
    using DSharpPlus;

    public interface IDiscordClientService
    {
        IReadOnlyDictionary<ulong, DiscordClient> DiscordClients { get; }

        bool Initialized { get; }

        Task Start();

        Task Stop();
    }
}