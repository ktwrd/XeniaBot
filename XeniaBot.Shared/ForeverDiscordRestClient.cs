using Discord.Rest;
using System;
using System.Threading.Tasks;

namespace XeniaBot.Shared;

public class ForeverDiscordRestClient : DiscordRestClient, IDisposable, IAsyncDisposable
{
    public ForeverDiscordRestClient()
        : base()
    { }

    public ForeverDiscordRestClient(DiscordRestConfig config)
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
