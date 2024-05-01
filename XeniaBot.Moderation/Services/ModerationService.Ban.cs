using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Moderation;
using XeniaBot.Data.Moderation.Models;
using XeniaBot.Data.Moderation.Repositories;
using XeniaBot.Moderation.Helpers;
using XeniaBot.Shared;

namespace XeniaBot.Moderation.Services;

public partial class ModerationService
{
	private LockState<ulong, ulong> ignoreBanLock = new LockState<ulong, ulong>();
	private LockState<ulong, ulong> ignoreUnBanLock = new LockState<ulong, ulong>();

	/// <summary>
	/// Member was banned.
	/// </summary>
	public event ModerationMemberBannedDelegate? MemberBanned;
	/// <summary>
	/// Member was unbanned.
	/// </summary>
	public event ModerationMemberUnbannedDelegate? MemberUnbanned;

    /// <summary>
    /// Ban a user from a guild.
    /// </summary>
    /// <param name="guild">Guild to ban the user in</param>
    /// <param name="targetUser">Target user to ban</param>
    /// <param name="actionedByUser">Who banned this user</param>
    /// <param name="reason">Reason why the user was banned</param>
    public async Task BanUser(SocketGuild guild, ulong targetUser, ulong actionedByUser, string? reason = null)
	{
		ignoreBanLock.Lock(guild.Id, targetUser);

		try
		{
			await guild.AddBanAsync(targetUser, reason: reason);
		}
		catch
		{
			throw;
		}
		finally
		{
			ignoreBanLock.Unlock(guild.Id, targetUser);
		}

		await AddRecordBan(guild, targetUser, actionedByUser);
	}
	/// <summary>
	/// Unban a user in a guild
	/// </summary>
	/// <param name="guild">Guild to unban the user in</param>
	/// <param name="targetUser">User to unban</param>
	/// <param name="actionedByUser">Who unbanned that user</param>
	/// <param name="reason">Why was that user unbanned</param>
	public async Task UnbanUser(SocketGuild guild, ulong targetUser, ulong actionedByUser, string? reason = null)
	{
		ignoreUnBanLock.Lock(guild.Id, targetUser);
		try
		{
			var rec = await guild.GetBanAsync(targetUser);
			if (rec != null)
				await guild.RemoveBanAsync(targetUser);
		}
		catch
		{
			throw;
		}
		finally
		{
			ignoreUnBanLock.Unlock(guild.Id, targetUser);
		}

		await AddRecordUnban(guild, targetUser, actionedByUser, reason);
	}

	/// <summary>
	/// Log in the database that the user was banned.
	/// </summary>
	/// <param name="guild"></param>
	/// <param name="targetUser"></param>
	/// <param name="actionedUser"></param>
	/// <returns></returns>
	protected async Task AddRecordBan(SocketGuild guild, ulong targetUser, ulong? actionedUser)
	{
		var guildBan = await guild.GetBanAsync(targetUser);

		var recordModel = new BanRecordModel()
		{
			GuildId = guild.Id.ToString(),
			CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
			UserId = targetUser.ToString(),
			ActionedByUserId = actionedUser?.ToString(),
			Reason = guildBan.Reason
		};
		var historyModel = new BanHistoryModel()
		{
			UserId = targetUser.ToString(),
			GuildId = guild.Id.ToString(),
			IsBanned = true,
			BanRecordId = recordModel.Id,
			Reason = guildBan.Reason
		};

        recordModel = await _banRecordRepo.InsertOrUpdate(recordModel);
        historyModel = await _banHistoryRepo.InsertOrUpdate(historyModel);

		MemberBanned?.Invoke(recordModel, historyModel);
    }
	protected async Task AddRecordUnban(SocketGuild guild, ulong targetUser, ulong? actionedUser, string? unbanReason = null)
	{
		var historyModel = new BanHistoryModel()
		{
			UserId = targetUser.ToString(),
			GuildId = guild.Id.ToString(),
			IsBanned = false,
			BanRecordId = null,
			Reason = unbanReason
		};

        historyModel = await _banHistoryRepo.InsertOrUpdate(historyModel);

		MemberUnbanned?.Invoke(historyModel);
	}

    private async Task DiscordClientUserBanned(SocketUser user, SocketGuild guild)
	{
		if (ignoreBanLock.IsLocked(guild.Id, user.Id))
		{
			return;
		}

		await AddRecordBan(guild, user.Id, null);
	}

	private async Task DiscordClientUserUnbanned(SocketUser user, SocketGuild guild)
	{
		if (ignoreUnBanLock.IsLocked(guild.Id, user.Id))
		{
			return;
		}

		await AddRecordUnban(guild, user.Id, null, null);
	}
}

