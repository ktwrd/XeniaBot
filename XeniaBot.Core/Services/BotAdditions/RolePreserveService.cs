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
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using ServerLogEvent = XeniaDiscord.Data.Models.ServerLog.ServerLogEvent;
using ServerLogRepository = XeniaDiscord.Data.Repositories.ServerLogRepository;
using XeniaDiscord.Data.Models.ServerLog;
using XeniaDiscord.Data.Models.Snapshot;
using XeniaDiscord.Data.Repositories;
using RolePreserveGuildRepository = XeniaDiscord.Data.Repositories.RolePreserveGuildRepository;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class RolePreserveService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(RolePreserveService));
    private readonly XeniaDbContext _db;
    private readonly ErrorReportService _err;
    private readonly DiscordSocketClient _client;
    private readonly ServerLogRepository _serverLogConfig;
    private readonly RolePreserveUserRepository _userRepository;
    private readonly RolePreserveGuildRepository _guildRepository;
    private readonly DiscordSnapshotService _snapshotService;
    private readonly ConfigData _configData;

    public RolePreserveService(IServiceProvider services)
        : base(services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _err = services.GetRequiredService<ErrorReportService>();
        _client = services.GetRequiredService<DiscordSocketClient>();
        _serverLogConfig = services.GetRequiredService<ServerLogRepository>();
        _userRepository = (scope?.ServiceProvider ?? services).GetRequiredService<RolePreserveUserRepository>();
        _guildRepository = (scope?.ServiceProvider ?? services).GetRequiredService<RolePreserveGuildRepository>();
        _snapshotService = (scope?.ServiceProvider ?? services).GetRequiredService<DiscordSnapshotService>();
        
        _client.UserJoined += ClientOnUserJoined;
        _snapshotService.GuildMemberUpdated += DiscordSnapshotOnGuildMemberUpdated;
    }

    private async Task DiscordSnapshotOnGuildMemberUpdated(
        GuildMemberSnapshotModel? before,
        GuildMemberSnapshotModel model)
    {
        if (model.RolesMatch(before)) return;

        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await _userRepository.UpdateSnapshot(db, model);
            
            await db.SaveChangesAsync();
            await trans.CommitAsync();
            _log.Trace($"Updated RolePreserveUser for UserId={model.UserId},GuildId={model.GuildId}");
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            // TODO error handling
            throw;
        }
    }
    
    private async Task SendFailureNotification(
        SocketGuildUser user,
        IReadOnlyCollection<ulong> success,
        IReadOnlyCollection<GuildMemberRoleSnapshotModel> fail)
    {
        if (fail.Count < 1) return;
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
            .WithTitle("Role Preserve - User Joined - " + user.Username)
            .WithFooter($"User Id: {user.Id}")
            .WithColor(new Color(255, 255, 255))
            .WithCurrentTimestamp();
        var failCount = fail.Count.ToString("n0");
        if (success.Count == 0)
        {
            embed.WithDescription($"- {Emotes.Warning} Failed to give user *any* roles");
            if (fail.Count > 0)
            {
                embed.Description += $" ({failCount})";
            }
        }
        else if (fail.Count > 0)
        {
            var failPlural = fail.Count == 1 ? "" : "s";
            if (fail.Count > 0) embed.Description += $"\n- Failed to add {failCount} role{failPlural}.";
        }

        var failField = string.Join("\n", fail.Select(v => $"- <@&{v.RoleId}>"));
        var failAttachment = string.Join("\n", fail.Select(v => $"{v.RoleId} - {v.GuildRoleSnapshot?.Name}"));

        var attachments = new List<FileAttachment>();
        switch (failField.Length)
        {
            case > 1024:
                attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(failAttachment)), "roles.txt"));
                embed.AddField("Failed Roles", "Too many roles failed! It's been attached as `roles.txt`");
                break;
            case > 0:
                embed.AddField("Failed Roles", failField);
                break;
        }

        foreach (var serverLogChannel in targetLogChannels)
        {
            SocketTextChannel? textChannel;
            try
            {
                textChannel = user.Guild.GetTextChannel(serverLogChannel.GetChannelId())
                    ?? throw new InvalidOperationException($"Channel {serverLogChannel.ChannelId} does not exist (GetTextChannel returned null)");
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Could not get channel {serverLogChannel.ChannelId} in Guild \"{user.Guild}\" ({user.Guild.Id}) from ServerLogChannel with Id={serverLogChannel.Id}");
                continue;
            }
            try
            {
                if (attachments.Count > 0)
                {
                    await textChannel.SendFilesAsync(attachments, embed: embed.Build());
                }
                else
                {
                    await textChannel.SendMessageAsync(embed: embed.Build());
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
        await using var db = _db.CreateSession();
        try
        {
            if (!await _guildRepository.IsEnabled(db, user.Guild.Id)) return;
            if (!await _userRepository.HasAny(db, user.Guild.Id, user.Id)) return;
            var roles = await _userRepository.FindRolesForUser(db, user.Guild.Id, user.Id);

            var ourHighestRoleEnumerable = user.Guild.CurrentUser.Roles.OrderByDescending(v => v.Position);
            var ourHighestRolePos = ourHighestRoleEnumerable.FirstOrDefault()?.Position ?? int.MinValue;

            var success = new List<ulong>();
            var fail = new List<GuildMemberRoleSnapshotModel>();
            foreach (var item in roles)
            {
                var roleId = item.GetRoleId();
                if (roleId == user.Guild.EveryoneRole.Id)
                    continue;
                try
                {
                    var existingRole = await ExceptionHelper.RetryOnTimedOut(async () => await user.Guild.GetRoleAsync(roleId));
                    // continue, since the role doesn't exist anymore
                    if (existingRole == null) continue;
                    if (existingRole.Position > ourHighestRolePos)
                    {
                        fail.Add(item);
                        continue;
                    }
                    await user.AddRoleAsync(roleId);
                    success.Add(roleId);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, $"Failed to grant Role {item} to User \"{user.Username}#{user.Discriminator}\" ({user.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id})");
                    fail.Add(item);
                }
            }

            if (fail.Count > 0)
            {
                await SendFailureNotification(user, success, fail);
            }
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
        new Thread((ThreadStart)delegate
        {
            try
            {
                PreserveAll().GetAwaiter().GetResult();
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
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var guild in _client.Guilds)
            {
                await PerformGuild(db, guild.Id);
            }
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            const string msg = "Failed to preserve all guilds";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg));
        }
    }

    private async Task PerformGuildChunk(XeniaDbContext db, ulong[] guildIds)
    {
        foreach (var id in guildIds)
        {
            await PerformGuild(db, id);
        }
    }

    private async Task PerformGuild(XeniaDbContext db, ulong guildId)
    {
        SocketGuild? guild;
        try
        {
            guild = _client.GetGuild(guildId);
            if (guild == null) return;
        }
        catch (Exception ex)
        {
            var msg = $"Failed to get Guild {guildId} for role preservation";
            _log.Warn(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg));
            return;
        }
        try
        {
            var result = await UseLatestSnapshotsForGuild(db, guild);
            if (result.IsFailure) throw new InvalidOperationException(result.Error);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to preserve roles for Guild \"{guild.Name}\" ({guild.Id})";
            _log.Warn(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithGuild(guild));
        }
    }
    
    public async Task PreserveGuild(SocketGuild guild)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var result = await UseLatestSnapshotsForGuild(db, guild);
            if (result.IsFailure) throw new InvalidOperationException(result.Error);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
        var memberCount = guild.Users.Count.ToString("n0");
        _log.Info($"Preserved all roles in Guild \"{guild.Name}\" ({guild.Id}), which archived {memberCount} members.");
    }
    
    public async Task<UnitResult<string>> UseLatestSnapshotsForGuild(
        XeniaDbContext db,
        ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        if (guild == null) return $"Guild does not exist: `{guildId}`";

        return await UseLatestSnapshotsForGuild(db, guild);
    }

    public async Task<UnitResult<string>> UseLatestSnapshotsForGuild(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        if (guild == null) return $"Guild does not exist: `{guildId}`";
        
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var result = await UseLatestSnapshotsForGuild(db, guild);
            if (result.IsFailure)
            {
                await trans.RollbackAsync();
                return result;
            }
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
        return UnitResult.Success<string>();
    }

    public async Task<UnitResult<string>> UseLatestSnapshotsForGuild(
        XeniaDbContext db,
        SocketGuild guild)
    {
        var guildIdStr = guild.Id.ToString();
        var userIds = guild.Users.Select(e => e.Id).ToHashSet();
        foreach (var userId in userIds)
        {
            var userIdStr = userId.ToString();
            var recordIdList = await db.GuildMemberSnapshots
                .Where(e => e.GuildId == guildIdStr && e.UserId == userIdStr)
                .OrderByDescending(e => e.RecordCreatedAt)
                .Select(e => e.RecordId)
                .Take(1)
                .ToListAsync();
            var recordId = Guid.Empty;
            if (recordIdList.Count > 0)
            {
                recordId = recordIdList[0];
            }
            else
            {
                // TODO get record id from DiscordSnapshotService
            }

            if (recordId == Guid.Empty)
            {
                // dafuq??
                continue;
            }

            await _userRepository.InsertOrUpdate(db, guild.Id, userId, recordId);
        }
        return UnitResult.Success<string>();
    }
}