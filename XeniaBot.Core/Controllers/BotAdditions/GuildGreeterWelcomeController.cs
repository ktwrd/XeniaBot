using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using XeniaBot.Core.Models;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Core.Controllers.BotAdditions;

[BotController]
public class GuildGreeterWelcomeController : BaseController
{
    private readonly GuildGreeterConfigController _configWelcomeController;
    private readonly DiscordSocketClient _discord;
    public GuildGreeterWelcomeController(IServiceProvider services) : base(services)
    {
        _configWelcomeController = services.GetRequiredService<GuildGreeterConfigController>();
        _discord = services.GetRequiredService<DiscordSocketClient>();

        _discord.UserJoined += _discord_UserJoined;
    }

    private async Task _discord_UserJoined(SocketGuildUser user)
    {
        var config = await _configWelcomeController.GetLatest(user.Guild.Id);
        if (config == null)
            return;
        if (config.GuildId != user.Guild.Id)
            return;
        if ((config?.ChannelId ?? 0) == 0)
            return;
        var channel = user.Guild.GetTextChannel(config?.ChannelId ?? 0);
        if (channel == null)
            return;
        var embed = config?.GetEmbed(user);
        if (config?.MentionNewUser ?? true)
        {
            await channel.SendMessageAsync($"<@{user.Id}>", embed: embed?.Build());
        }
        else
        {
            await channel.SendMessageAsync(embed: embed?.Build());
        }
    }
}