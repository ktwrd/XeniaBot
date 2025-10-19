using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace XeniaBot.Shared;

public class ForeverDiscordShardedClient : DiscordShardedClient, IDisposable, IAsyncDisposable
{
    /// <inheritdoc/>
    public ForeverDiscordShardedClient()
        : base()
    { }

    /// <inheritdoc/>
    public ForeverDiscordShardedClient(
        DiscordSocketConfig config)
        : base(config)
    { }

    /// <inheritdoc/>
    public ForeverDiscordShardedClient(
        int[] ids,
        DiscordSocketConfig config)
        : base(ids, config)
    { }

    public new void Dispose()
    {
        // don't do anything
    }

    public new ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
