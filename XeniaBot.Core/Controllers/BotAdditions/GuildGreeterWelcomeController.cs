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
public class GuildGreeterWelcomeController : BaseController, IFlightCheckValidator
{
    private readonly GuildGreeterConfigController _configWelcomeController;
    private readonly DiscordSocketClient _discord;
    public GuildGreeterWelcomeController(IServiceProvider services) : base(services)
    {
        _configWelcomeController = services.GetRequiredService<GuildGreeterConfigController>();
        _discord = services.GetRequiredService<DiscordSocketClient>();

        _discord.UserJoined += _discord_UserJoined;
    }

    public async Task<FlightCheckValidationResult> FlightCheckGuild(SocketGuild guild)
    {
        var welcomeConfig = await _configWelcomeController.GetLatest(guild.Id);
        if (welcomeConfig == null || (welcomeConfig.ChannelId ?? 0) == 0)
            return new FlightCheckValidationResult(true);

        try
        {
            guild.GetChannel((ulong)welcomeConfig.ChannelId);
        }
        catch (Exception ex)
        {
            return new FlightCheckValidationResult(false, new EmbedFieldBuilder()
                .WithName("Guild Greeter - Welcome")
                .WithValue(string.Join("\n", new string[]
                {
                    $"Failed to fetch channel {DiscordURLHelper.GuildChannel(guild.Id, (ulong)welcomeConfig.ChannelId)}",
                    "```",
                    ex.Message,
                    "```"
                })));
        }

        if (!DiscordHelper.CanAccessChannel(_discord, guild.GetChannel((ulong)welcomeConfig.ChannelId)))
        {
            return new FlightCheckValidationResult(false, new EmbedFieldBuilder()
                .WithName("Guild Greeter - Welcome")
                .WithValue(string.Join("\n", new string[]
                {
                    $"Unable to send messages in {DiscordURLHelper.GuildChannel(guild.Id, (ulong)welcomeConfig.ChannelId)}."
                })));
        }

        return new FlightCheckValidationResult(true);
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