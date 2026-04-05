using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.Shared.Helpers;
using System.Threading;
using NLog;

using ServerLogEvent = XeniaDiscord.Data.Models.ServerLog.ServerLogEvent;
using ServerLogRepository = XeniaDiscord.Data.Repositories.ServerLogRepository;
using XeniaDiscord.Data.Models.ServerLog;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class RolePreserveService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(RolePreserveService));
    private readonly DiscordSocketClient _client;
    private readonly RolePreserveRepository _config;
    private readonly RolePreserveGuildRepository _guildConfig;
    private readonly ErrorReportService _err;
    private readonly ServerLogRepository _serverLogConfig;
    private readonly ConfigData _configData;
    public RolePreserveService(IServiceProvider services)
        : base(services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<RolePreserveRepository>();
        _configData = services.GetRequiredService<ConfigData>();
        _err = services.GetRequiredService<ErrorReportService>();
        _serverLogConfig = services.GetRequiredService<ServerLogRepository>();
        _guildConfig = services.GetRequiredService<RolePreserveGuildRepository>();
        
        _client.GuildMemberUpdated += (cacheable, user) => PreserveGuildMember(user.Guild.Id, user.Id);
        _client.UserJoined += ClientOnUserJoined;
    }

    private async Task SendFailureNotification(
        SocketGuildUser user,
        IReadOnlyCollection<ulong> success,
        IReadOnlyCollection<ulong> fail)
    {
        IReadOnlyCollection<ServerLogChannelModel> targetLogChannels;
        try
        {
            targetLogChannels = await _serverLogConfig.GetChannelsForGuild(user.Guild.Id, [ServerLogEvent.MemberJoin], new()
            {
                IgnoreDisabledGuilds = true
            });
            if (targetLogChannels.Count < 1) return;
        }
        catch (Exception ex)
        {
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to get Server Log Channel models with event {ServerLogEvent.MemberJoin} for Guild \"{user.Guild.Name}\" ({user.Guild.Id})")
                .WithUser(user)
                .WithGuild(user.Guild));
            return;
        }

        var successCount = success.Count.ToString("n0");
        var successPlural = success.Count == 1 ? "" : "s";
        var embed = new EmbedBuilder()
            .WithDescription($"Added {successCount} role{successPlural} successfully.")
            .WithTitle($"Role Preserve - User Joined")
            .WithColor(new Color(255, 255, 255))
            .WithCurrentTimestamp();
        var failCount = fail.Count.ToString("n0");
        var failPlural = fail.Count == 1 ? "" : "s";
        if (fail.Count > 0)
            embed.Description += $"\nFailed to add {failCount} role{failPlural}.";

        var f = string.Join("\n", fail.Select(v => $"- <@&{v}>"));

        foreach (var serverLogChannel in targetLogChannels)
        {
            SocketTextChannel? textChannel;
            try
            {
                textChannel = user.Guild.GetTextChannel(serverLogChannel.GetChannelId())
                    ?? throw new InvalidOperationException($"Channel does not exist (GetTextChannel returned null)");
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Could not find channel {serverLogChannel.ChannelId} in Guild \"{user.Guild}\" ({user.Guild.Id}) from ServerLogChannel with Id={serverLogChannel.Id}");
                continue;
            }
            try
            {
                if (fail.Count > 0)
                {
                    if (f.Length > 1000)
                    {
                        var stream = new MemoryStream(Encoding.UTF8.GetBytes(f));
                        await textChannel.SendFileAsync(stream, filename: "failed.txt", embed: embed.Build());
                    }
                    else
                    {
                        embed.AddField($"Failed to add following roles", f);
                        await textChannel.SendMessageAsync(embed: embed.Build());
                    }
                }
            }
            catch (Exception ex)
            {
                await _err.Submit(new ErrorReportBuilder()
                    .WithException(ex)
                    .WithNotes($"Failed to send message in channel \"{textChannel.Name}\" ({textChannel.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id}) for user \"{user.Username}#{user.Discriminator}\" ({user.Id})")
                    .WithUser(user)
                    .WithGuild(user.Guild)
                    .WithChannel(textChannel)
                    .AddSerializedAttachment("serverLogChannel.json", serverLogChannel));
            }
        }
    }

    private async Task ClientOnUserJoined(SocketGuildUser user)
    {
        try
        {
            var guildModel = await _guildConfig.Get(user.Guild.Id);
            if (guildModel?.Enable != true)
                return;
            var model = await _config.Get(user.Id, user.Guild.Id);
            if (model == null)
                return;

            var ourHighestRoleEnumerable = user.Guild.CurrentUser.Roles.OrderByDescending(v => v.Position);
            var ourHighestRolePos = ourHighestRoleEnumerable.FirstOrDefault()?.Position ?? int.MinValue;

            var success = new List<ulong>();
            var fail = new List<ulong>();
            foreach (var item in model.Roles)
            {
                if (item == user.Guild.EveryoneRole.Id || item == 0)
                    continue;
                try
                {
                    var existingRole = await user.Guild.GetRoleAsync(item);
                    if (existingRole.Position > ourHighestRolePos)
                    {
                        fail.Add(item);
                        continue;
                    }
                    await user.AddRoleAsync(item);
                    success.Add(item);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, $"Failed to grant Role {item} to User \"{user.Username}#{user.Discriminator}\" ({user.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id})");
                    fail.Add(item);
                }
            }

            await SendFailureNotification(user, success, fail);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to restore roles for User \"{user.Username}#{user.Discriminator}\" ({user.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id})";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(user)
                .WithGuild(user.Guild));
        }
    }

    public override Task OnReadyDelay()
    {
        if (!_configData.RefreshRolePreserveOnStart)
        {
            _log.Info($"Not going to run {nameof(PreserveAll)} since {nameof(_configData.RefreshRolePreserveOnStart)} is set to false");
            return Task.CompletedTask;
        }
        new Thread((ThreadStart)async delegate
        {
            try
            {
                await PreserveAll();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to run {nameof(PreserveAll)}");
            }
        }).Start();
        return Task.CompletedTask;
    }

    public async Task PreserveAll()
    {
        try
        {
            var taskList = new List<Task>();
            var chunkedIds = ArrayHelper.Chunk(_client.Guilds.Select(v => v.Id).ToArray(), 10);
            await Task.WhenAll(chunkedIds.Select(PerformGuild));
        }
        catch (Exception ex)
        {
            const string msg = "Failed to preserve all guilds";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg));
        }

        async Task PerformGuild(ulong[] guildIds)
        {
            foreach (var id in guildIds)
            {
                SocketGuild? guild;
                try
                {
                    guild = _client.GetGuild(id);
                    if (guild == null) continue;
                }
                catch (Exception ex)
                {
                    var msg = $"Failed to get Guild {id} for role preservation";
                    _log.Warn(ex, msg);
                    await _err.Submit(new ErrorReportBuilder()
                        .WithException(ex)
                        .WithNotes(msg));
                    continue;
                }
                try
                {
                    await PreserveGuild(guild);
                }
                catch (Exception ex)
                {
                    var msg = $"Failed to preserver Guild \"{guild.Name}\" ({guild.Id})";
                    _log.Warn(ex, msg);
                    await _err.Submit(new ErrorReportBuilder()
                        .WithException(ex)
                        .WithNotes(msg)
                        .WithGuild(guild));
                }
            }
        }
    }
    
    public async Task PreserveGuild(SocketGuild guild)
    {
        await Task.WhenAll(guild.Users.Select(u => PreserveGuildMember(guild.Id, u.Id)));
        var memberCount = guild.Users.Count.ToString("n0");
        _log.Info($"Preserved all roles in Guild \"{guild.Name}\" ({guild.Id}), which archived {memberCount} members.");
    }

    public async Task PreserveGuildMember(ulong guildId, ulong userId)
    {
        try
        {
            var guild = _client.GetGuild(guildId);
            var member = guild.GetUser(userId);

            var roleIdList = member.Roles.Select(v => v.Id);

            var model = new RolePreserveModel()
            {
                UserId = userId,
                GuildId = guildId,
                Roles = [..roleIdList]
            };
            await _config.Set(model);
        }
        catch (Exception ex)
        {
            await _err.ReportException(ex, $"Failed to preserve member {userId} in guild {guildId}");
        }
    }
}