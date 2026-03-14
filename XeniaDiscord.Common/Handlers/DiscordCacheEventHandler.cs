using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared;
using XeniaDiscord.Common.Services;

namespace XeniaDiscord.Common.Handlers;

public class DiscordCacheEventHandler : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly DiscordCacheService _cache;
    private readonly DiscordSocketClient _client;
    public DiscordCacheEventHandler(IServiceProvider services) : base(services)
    {
        _cache = services.GetRequiredService<DiscordCacheService>();
        _client = services.GetRequiredService<DiscordSocketClient>();

        if (services.GetRequiredService<ProgramDetails>().Platform == XeniaPlatform.Bot)
        {
            _client.JoinedGuild += OnGuildJoined;
            _client.GuildUpdated += OnGuildUpdated;

            _client.UserJoined += OnUserJoined;
            _client.UserLeft += OnUserLeft;
            _client.UserUpdated += OnUserUpdated;
            _client.GuildMemberUpdated += OnGuildMemberUpdated;
        }
    }

    private async Task OnGuildJoined(SocketGuild guild)
    {
        try
        {
            await _cache.UpdateGuild(guild);
        }
        catch (Exception ex)
        {
            _log.Fatal(ex, $"Failed to update Guild \"{guild.Name}\" ({guild.Id})");
        }
    }

    private async Task OnGuildUpdated(SocketGuild before, SocketGuild after)
    {
        try
        {
            await _cache.UpdateGuild(after);
        }
        catch (Exception ex)
        {
            _log.Fatal(ex, $"Failed to update Guild \"{after.Name}\" ({after.Id})");
        }
    }

    private async Task OnGuildMemberUpdated(
        Discord.Cacheable<SocketGuildUser, ulong> before,
        SocketGuildUser user)
    {
        try
        {
            await _cache.UpdateGuildMember(user.Guild, user);
        }
        catch (Exception ex)
        {
            _log.Fatal(ex, $"Failed to update Member \"{user.GlobalName}\" ({user.Username}, {user.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id})");
        }
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        try
        {
            await _cache.UpdateGuildMember(user.Guild, user);
        }
        catch (Exception ex)
        {
            _log.Fatal(ex, $"Failed to update Member \"{user.GlobalName}\" ({user.Username}, {user.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id})");
        }
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        try
        {
            await _cache.UpdateGuildMember(guild, user);
        }
        catch (Exception ex)
        {
            _log.Fatal(ex, $"Failed to update Member \"{user.GlobalName}\" ({user.Username}, {user.Id}) in Guild \"{guild.Name}\" ({guild.Id})");
        }
    }

    private async Task OnUserUpdated(SocketUser before, SocketUser after)
    {
        try
        {
            await _cache.UpdateUser(after);
        }
        catch (Exception ex)
        {
            _log.Fatal(ex, $"Failed to update user \"{after}\" ({after.Id})");
        }
    }
}
