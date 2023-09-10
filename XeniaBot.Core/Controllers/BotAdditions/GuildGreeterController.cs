﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers.BotAdditions;

[BotController]
public class GuildGreeterController : BaseController
{
    private readonly GuildGreeterConfigController _configController;
    private readonly DiscordSocketClient _discord;
    public GuildGreeterController(IServiceProvider services) : base(services)
    {
        _configController = services.GetRequiredService<GuildGreeterConfigController>();
        _discord = services.GetRequiredService<DiscordSocketClient>();

        _discord.UserJoined += _discord_UserJoined;
    }

    private async Task _discord_UserJoined(SocketGuildUser user)
    {
        var config = await _configController.GetLatest(user.Guild.Id);
        if (config == null)
            return;
        if (config.GuildId != user.Guild.Id)
            return;
        if (config.ChannelId == null || config.ChannelId == 0)
            return;
        var channel = user.Guild.GetTextChannel(config.ChannelId ?? 0);
        if (channel == null)
            return;
        var embed = config.GetEmbed(user);
        if (config.MentionNewUser)
        {
            await channel.SendMessageAsync($"<@{user.Id}>", embed: embed.Build());
        }
        else
        {
            await channel.SendMessageAsync(embed: embed.Build());
        }
    }
}