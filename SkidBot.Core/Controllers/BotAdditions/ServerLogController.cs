using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Helpers;
using SkidBot.Core.Models;
using SkidBot.Shared;

namespace SkidBot.Core.Controllers.BotAdditions;

[SkidController]
public class ServerLogController : BaseController
{
    private ServerLogConfigController _config;
    private DiscordSocketClient _discord;
    public ServerLogController(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<ServerLogConfigController>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
    }

    public override async Task InitializeAsync()
    {
        _discord.UserJoined += Event_UserJoined;
        _discord.UserLeft += Event_UserLeave;
    }
    private async Task EventHandle(ulong serverId, Func<ServerLogModel, ulong?> selectChannel, EmbedBuilder embed)
    {
        var data = await _config.Get(serverId);

        // Server not setup for logs, aborting.
        if (data == null)
            return;

        var targetChannel = selectChannel(data) ?? data.DefaultLogChannel;

        var server = _discord.GetGuild(serverId);
        var logChannel = server.GetTextChannel(targetChannel);

        await logChannel.SendMessageAsync(embed: embed.Build());
    }
    
    private async Task Event_UserJoined(SocketGuildUser user)
    {
        var userSafe = user.Username.Replace("`", "\\`");
        var embed = new EmbedBuilder()
            .WithTitle("User Joined")
            .WithDescription($"<@{user.Id}>" + string.Join("\n", new string[]
            {
                "```",
                $"{userSafe}#{user.Discriminator}",
                $"ID: {user.Id}",
                "```",
            }))
            .AddField("Account Age", string.Join("\n", new string[]
            {
                TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                $"`{user.CreatedAt}`"
            }))
            .WithThumbnailUrl(user.GetAvatarUrl())
            .WithColor(Color.Green);

        await EventHandle(user.Guild.Id, (v) => v.JoinChannel, embed);
    }
    private async Task Event_UserLeave(SocketGuild guild, SocketUser user)
    {
        var userSafe = user.Username.Replace("`", "\\`");
        var embed = new EmbedBuilder()
            .WithTitle("User Left")
            .WithDescription($"<@{user.Id}>" + string.Join("\n", new string[]
            {
                "```",
                $"{userSafe}#{user.Discriminator}",
                $"ID: {user.Id}",
                "```",
            }))
            .AddField("Account Age", string.Join("\n", new string[]
            {
                TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                $"`{user.CreatedAt}`"
            }))
            .WithThumbnailUrl(user.GetAvatarUrl())
            .WithColor(Color.Red);

        await EventHandle(guild.Id, (v) => v.LeaveChannel, embed);
    }
}