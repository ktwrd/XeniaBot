using CSharpFunctionalExtensions;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

using AuditLogEntryResult = CSharpFunctionalExtensions.Result<Discord.IAuditLogEntry?, XeniaDiscord.Common.Services.DiscordAuditLogService.ErrorCode>;

namespace XeniaDiscord.Common.Services;

public sealed class DiscordAuditLogService : BaseService
{
    private readonly DiscordSocketClient _discord;
    public DiscordAuditLogService(IServiceProvider services) : base(services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
    }

    public async Task<AuditLogEntryResult> GetLatestBanEvent(
        ulong guildId,
        ulong targetUserId,
        bool doneRecently = false)
    {
        var doneWithinTheLast = doneRecently
            ? TimeSpan.FromMinutes(5)
            : Maybe<TimeSpan>.None;
        return await GetLatestBanEvent(guildId, targetUserId, doneWithinTheLast);
    }

    public async Task<AuditLogEntryResult> GetLatestBanEvent(
        ulong guildId,
        ulong targetUserId,
        Maybe<TimeSpan> doneWithinTheLast)
    {
        return await GetLatest(guildId, ActionType.Ban, doneWithinTheLast, auditLogEntry =>
        {
            return (auditLogEntry.Data is BanAuditLogData data && data.Target.Id == targetUserId)
                || (auditLogEntry.Data is SocketBanAuditLogData socketData && socketData.Target.Id == targetUserId);
        });
    }

    public async Task<AuditLogEntryResult> GetLatestKickEvent(
        ulong guildId,
        ulong targetUserId,
        Maybe<TimeSpan> doneWithinTheLast)
    {
        return await GetLatest(guildId, ActionType.Kick, doneWithinTheLast, auditLogEntry =>
        {
            return (auditLogEntry.Data is KickAuditLogData data && data.Target.Id == targetUserId)
                || (auditLogEntry.Data is SocketKickAuditLogData socketData && socketData.Target.Id == targetUserId);
        });
    }

    public enum ErrorCode
    {
        GuildNotFound,
        [Description("Missing permission: View Audit Log")]
        MissingPermissions_ViewAuditLog
    }

    private async Task<AuditLogEntryResult> GetLatest(ulong guildId, ActionType action, Maybe<TimeSpan> doneWithinTheLast, Func<IAuditLogEntry, bool> predicate)
    {
        var guild = await ExceptionHelper.RetryOnTimedOut(async () => _discord.GetGuild(guildId));
        if (guild == null)
        {
            return ErrorCode.GuildNotFound;
        }

        DateTimeOffset? start = doneWithinTheLast.HasValue ? DateTimeOffset.UtcNow - doneWithinTheLast.Value : null;

        try
        {
            return await ExceptionHelper.RetryOnTimedOut(async () =>
            {
                await foreach (var page in guild.GetAuditLogsAsync(1_000_000, actionType: action))
                {
                    foreach (var item in page)
                    {
                        if (start.HasValue && item.CreatedAt < start.Value) continue;
                        if (predicate(item))
                        {
                            return item;
                        }
                    }
                }
                return null;
            });
        }
        catch (Exception ex)
        {
            if (ex.IsMissingDiscordPermissions())
            {
                return ErrorCode.MissingPermissions_ViewAuditLog;
            }
            throw;
        }
    }
}
