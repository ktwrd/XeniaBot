using Discord.WebSocket;
using XeniaBot.Data.Moderation.Models;
using XeniaBot.Shared;

namespace XeniaBot.Moderation.Services;

public partial class ModerationService
{
    private LockState<ulong, ulong> ignoreKickLock = new LockState<ulong, ulong>();

    public async Task KickUser(SocketGuild guild, ulong targetUser, ulong? actionedByUser, string? reason = null)
    {
        ignoreKickLock.Lock(guild.Id, targetUser);

        try
        {
            var member = guild.GetUser(targetUser);
            await member.KickAsync(reason);
        }
        catch
        {
            ignoreKickLock.Unlock(guild.Id, targetUser);
            throw;
        }

        await AddRecordKick(guild, targetUser, actionedByUser, reason);
    }

    protected async Task AddRecordKick(SocketGuild guild, ulong targetUser, ulong? actionedUser, string? reason, long? timestamp = null)
    {
        var recordModel = new KickRecordModel()
        {
            GuildId = guild.Id.ToString(),
            UserId = targetUser.ToString(),
            ActionedByUserId = actionedUser?.ToString(),
            Reason = reason,
            CreatedAt = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        await _kickRecordRepo.InsertOrUpdate(recordModel);
    }
}