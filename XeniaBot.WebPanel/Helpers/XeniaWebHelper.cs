using System;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeniaBot.DiscordCache.Helpers;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Helpers;

public class XeniaWebHelper
{
    private readonly ErrorReportService _err;
    private readonly DiscordShardedClient _discord;
    private readonly DiscordCacheHelper _cacheHelper;
    private readonly ILogger<XeniaWebHelper> _logger;
    
    public XeniaWebHelper(IServiceProvider services, ILogger<XeniaWebHelper> logger)
    {
        _logger = logger;
        _err = services.GetRequiredService<ErrorReportService>();
        _discord = services.GetRequiredService<DiscordShardedClient>();
        _cacheHelper = services.GetRequiredService<DiscordCacheHelper>();
    }
    public string GetGuildImage(ulong guildId)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        if (guild == null)
            return "/DebugEmpty.png";
        return guild.IconUrl;
    }

    public string GetGuildName(ulong guildId)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        if (string.IsNullOrWhiteSpace(guild?.Name))
            return guildId.ToString();
        return guild.Name;
    }
    
    public string GetChannelName(ulong guildId, ulong channelId)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        if (guild == null)
            return channelId.ToString();

        foreach (var i in guild.Channels)
        {
            if (i.Id == channelId)
                return i.Name;
        }

        return channelId.ToString();
    }
    
    
    public bool CanAccessGuild(
        ulong guildId,
        ulong userId,
        GuildPermission permissionRequired = GuildPermission.ManageGuild)
    {
        try
        {
            var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
            if (guild == null) return false;
            var guildUser = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(userId));
            if (guildUser == null)
                return false;

            return guildUser.GuildPermissions.Has(permissionRequired);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run (guild={GuildId}, user={UserId}, permissions={PermissionRequired})", guildId, userId, permissionRequired);
            _err.ReportException(
                ex, $"Failed to run AspHelper.CanAccessGuild ({guildId}, {userId}, {permissionRequired})").GetAwaiter().GetResult();
            return false;
        }
    }
    
    public string GetUserProfilePicture(ulong userId)
    {
        var user = _cacheHelper.TryGetUser(userId).GetAwaiter().GetResult();
        if (user == null)
        {
            return "/Debugempty.png";
        }
        else
        {
            return user.GetDisplayAvatarUrl() ?? "/Debugempty.png";
        }
    }

    public string GetUserProfilePicture(SocketGuildUser guildUser)
    {
        return guildUser.GetGuildAvatarUrl()
               ?? GetUserProfilePicture(guildUser.Id);
    }
}