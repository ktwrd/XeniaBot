using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class GuildGreeterService : BaseService
{
    private readonly GuildGreeterConfigRepository _configWelcomeRepository;
    private readonly GuildGreetByeConfigRepository _configByeRepository;
    private readonly UserConfigRepository _userConfigRepo;
    private readonly DiscordSocketClient _discord;
    private readonly ErrorReportService _errReportService;
    public GuildGreeterService(IServiceProvider services) : base(services)
    {
        _configWelcomeRepository = services.GetRequiredService<GuildGreeterConfigRepository>();
        _configByeRepository = services.GetRequiredService<GuildGreetByeConfigRepository>();
        _userConfigRepo = services.GetRequiredService<UserConfigRepository>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _errReportService = services.GetRequiredService<ErrorReportService>();

        _discord.UserJoined += _discord_UserJoined;
        _discord.UserLeft += _discord_UserLeft;
    }

    private async Task _discord_UserJoined(SocketGuildUser user)
    {
        var userConfig = await _userConfigRepo.Get(user.Id);
        userConfig ??= new();
        var config = await _configWelcomeRepository.GetLatest(user.Guild.Id);
        if (config == null)
            return;
        if (config.GuildId != user.Guild.Id)
            return;
        if ((config.ChannelId ?? 0) == 0)
            return;
        IMessageChannel? channel = user.Guild.GetTextChannel(config.ChannelId ?? 0);
        if (channel == null)
            return;
        var embed = config.GetEmbed(user);
        string? createDMChannelError = null;
        if (userConfig.SilentJoinMessage)
        {
            try
            {
                var dmChannel = await user.CreateDMChannelAsync();
                if (dmChannel == null)
                {
                    createDMChannelError = "Couldn't create DM Channel";
                    throw new Exception($"{nameof(user.CreateDMChannelAsync)} returned null");
                }
                await dmChannel.SendMessageAsync($"Greeter Message from {user.Guild.Name} ({user.Guild.Id})", embed: embed.Build());
                return;
            }
            catch (Exception ex)
            {
                var msg = $"Failed to create DM Channel for User {user.ToString()} ({user.Id}";
                Log.Error($"{msg}\n{ex}");
                try
                {
                    await _errReportService.ReportException(ex, msg);
                }
                catch (Exception iex)
                {
                    Log.Error($"Failed to report exception\n{iex}");
                }
                createDMChannelError = ex.Message;
            }
        }
        if (!string.IsNullOrEmpty(createDMChannelError))
        {
            embed.AddField("Error", $"Couldn't send Greeter message in DMs\n```\n{createDMChannelError}\n```");
        }
        if (config?.MentionNewUser ?? true)
        {
            await channel.SendMessageAsync($"<@{user.Id}>", embed: embed.Build());
        }
        else
        {
            await channel.SendMessageAsync(embed: embed.Build());
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