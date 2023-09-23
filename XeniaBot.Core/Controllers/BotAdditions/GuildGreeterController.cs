using System;
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
    private readonly GuildGreeterConfigController _configWelcomeController;
    private readonly GuildGreetByeConfigController _configByeController;
    private readonly DiscordSocketClient _discord;
    public GuildGreeterController(IServiceProvider services) : base(services)
    {
        _configWelcomeController = services.GetRequiredService<GuildGreeterConfigController>();
        _configByeController = services.GetRequiredService<GuildGreetByeConfigController>();
        _discord = services.GetRequiredService<DiscordSocketClient>();

        _discord.UserJoined += _discord_UserJoined;
        _discord.UserLeft += _discord_UserLeft;
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

    private async Task _discord_UserLeft(SocketGuild guild, SocketUser user)
    {
        var config = await _configByeController.GetLatest(guild.Id);
        if (config == null)
            return;
        if (config.GuildId != guild.Id)
            return;
        if ((config?.ChannelId ?? 0) == 0)
            return;
        var channel = guild.GetTextChannel(config.ChannelId ?? 0);
        if (channel == null)
            return;
        var embed = config.GetEmbed(user, guild);
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