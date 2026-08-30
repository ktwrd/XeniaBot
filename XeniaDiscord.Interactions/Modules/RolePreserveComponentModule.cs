using System.ComponentModel;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared;
using XeniaDiscord.Data;
using XeniaDiscord.Interactions.Helpers;

namespace XeniaDiscord.Interactions.Modules;

public class RolePreserveComponentModule : InteractionModuleBase<ShardedInteractionContext<SocketMessageComponent>>
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly IDbContextFactory<XeniaDbContext> _dbFactory;
    private readonly ConfigData _config;
    public RolePreserveComponentModule(IServiceProvider services)
    {
        try
        {
            _dbFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            throw new InvalidOperationException("Failed to get services", ex);
        }

        _config = services.GetRequiredService<ConfigData>();
    }

    [ComponentInteraction(RolePreserveModuleHelper.ViewBlacklistedRolesInteractionName)]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [UsedImplicitly]
    public async Task ListComponent(
        [MinValue(1)]
        [DefaultValue(1)]
        int page)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var (embed, components) = await RolePreserveModuleHelper.ListEmbed(_config, db, Context.Guild, page);
            await Context.Interaction.UpdateAsync(
                p =>
                {
                    p.Embed = embed.Build();
                    p.Components = components == null
                        ? Optional<MessageComponent>.Unspecified
                        : components.Build();
                });
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
    }
}