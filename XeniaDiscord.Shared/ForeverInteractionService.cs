using Discord.Interactions;
using Discord.Rest;
using System;

namespace XeniaBot.Shared;

public class ForeverInteractionService : InteractionService, IDisposable
{
    public ForeverInteractionService(DiscordRestClient discord, InteractionServiceConfig? config = null)
        : base(discord, config)
    { }

    public ForeverInteractionService(IRestClientProvider discordProvider, InteractionServiceConfig? config = null)
        : base(discordProvider, config)
    { }

    public new void Dispose()
    {
        // don't do anything
    }
}
