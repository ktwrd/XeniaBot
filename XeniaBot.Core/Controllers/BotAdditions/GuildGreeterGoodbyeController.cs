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
public class GuildGreeterGoodbyeController : BaseController, IFlightCheckValidator
{
    private readonly GuildGreetByeConfigController _configByeController;
    private readonly DiscordSocketClient _discord;
    public GuildGreeterGoodbyeController(IServiceProvider services) : base(services)
    {
        _configByeController = services.GetRequiredService<GuildGreetByeConfigController>();
        _discord = services.GetRequiredService<DiscordSocketClient>();

        _discord.UserLeft += _discord_UserLeft;
    }

    public async Task<FlightCheckValidationResult> FlightCheckGuild(SocketGuild guild)
    {
        var goodbyeConfig = await _configByeController.GetLatest(guild.Id);
        if (goodbyeConfig?.ChannelId == null || (goodbyeConfig.ChannelId ?? 0) == 0)
            return new FlightCheckValidationResult(true);

        var lines = new List<string>();
        try
        {
            guild.GetChannel((ulong)goodbyeConfig.ChannelId);
        }
        catch (Exception ex)
        {
            return new FlightCheckValidationResult(false, new EmbedFieldBuilder()
                .WithName("Guild Greeter - Goodbye")
                .WithValue(string.Join("\n", new string[]
                {
                    $"Failed to fetch channel {DiscordURLHelper.GuildChannel(guild.Id, (ulong)goodbyeConfig.ChannelId)}",
                    "```",
                    ex.Message,
                    "```"
                })));
        }

        if (!DiscordHelper.CanAccessChannel(_discord, guild.GetChannel((ulong)goodbyeConfig.ChannelId)))
        {
            return new FlightCheckValidationResult(false, new EmbedFieldBuilder()
                .WithName("Guild Greeter - Goodbye")
                .WithValue(string.Join("\n", new string[]
                {
                    $"Unable to send messages in {DiscordURLHelper.GuildChannel(guild.Id, (ulong)goodbyeConfig.ChannelId)}."
                })));
        }

        return new FlightCheckValidationResult(true);
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