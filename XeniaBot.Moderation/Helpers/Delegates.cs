using Discord.WebSocket;
using XeniaBot.Data.Moderation.Models;

namespace XeniaBot.Moderation.Helpers;

/// <summary>
/// Used when notifying that a member was kicked.
/// </summary>
public delegate Task MemberKickedDelegate(
    SocketGuild guild,
    ulong userId,
    ulong actionedByUserId,
    string? reason,
    DateTimeOffset timestamp);

/// <summary>
/// Used for when notifying that a member was banned.
/// </summary>
public delegate Task MemberBannedDelegate(
    SocketGuild guild,
    ulong userId,
    ulong actionedByUserId,
    string? reason,
    DateTimeOffset timestamp);

/// <summary>
/// Used when notifying that a member was banned.
/// </summary>
public delegate Task ModerationMemberBannedDelegate(BanRecordModel record, BanHistoryModel history);

/// <summary>
/// Used when notifying that a member was unbanned.
/// </summary>
public delegate Task ModerationMemberUnbannedDelegate(BanHistoryModel history);