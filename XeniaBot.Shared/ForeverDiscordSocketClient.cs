using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace XeniaBot.Shared;

public class ForeverDiscordSocketClient : DiscordSocketClient, IDisposable, IAsyncDisposable
{
    public ForeverDiscordSocketClient()
        : base()
    { }

    public ForeverDiscordSocketClient(DiscordSocketConfig config)
        : base(config)
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
