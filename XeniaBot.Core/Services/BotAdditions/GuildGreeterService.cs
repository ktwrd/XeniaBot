using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class GuildGreeterService : BaseService
{
    private readonly GuildGreeterConfigRepository _configWelcomeRepository;
    private readonly GuildGreetByeConfigRepository _configByeRepository;
    private readonly DiscordSocketClient _discord;
    public GuildGreeterService(IServiceProvider services) : base(services)
    {
        _configWelcomeRepository = services.GetRequiredService<GuildGreeterConfigRepository>();
        _configByeRepository = services.GetRequiredService<GuildGreetByeConfigRepository>();
        _discord = services.GetRequiredService<DiscordSocketClient>();

        _discord.UserJoined += _discord_UserJoined;
        _discord.UserLeft += _discord_UserLeft;
    }

    private async Task _discord_UserJoined(SocketGuildUser user)
    {
        var config = await _configWelcomeRepository.GetLatest(user.Guild.Id);
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
        var config = await _configByeRepository.GetLatest(guild.Id);
        if (config == null)
            return;
        if (config.GuildId != guild.Id)
            return;
        if ((config?.ChannelId ?? 0) == 0)
            return;
        var channel = guild.GetTextChannel(config?.ChannelId ?? 0);
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