using Discord.WebSocket;

namespace XeniaBot.Moderation.Helpers;

public delegate Task UserKickedDelegate(SocketGuild guildId, ulong userId, ulong actionedByUserId, string? reason, DateTimeOffset timestamp);