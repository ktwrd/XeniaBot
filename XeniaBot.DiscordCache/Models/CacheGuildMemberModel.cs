using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheGuildMemberModel : CacheUserModel
{
    public new const string CollectionName = "cache_store_guild_member";
    public ulong GuildId { get; set; }
    public string DisplayName => Nickname ?? GlobalName ?? Username;
    [BsonIgnoreIfNull]
    public string? Nickname { get; set; }
    public string DisplayAvatarId => GuildAvatarId ?? AvatarId;
    [BsonIgnoreIfNull]
    public string? GuildAvatarId { get; set; }
    public CacheGuildPermissions GuildPermissions { get; set; }
    public bool IsSelfDeafened { get; set; }
    public bool IsSelfMuted { get; set; }
    public bool IsSuppressed { get; set; }
    public bool IsDeafened { get; set; }
    public bool IsMuted { get; set; }
    public bool IsStreaming { get; set; }
    public bool IsVideoing { get; set; }
    [BsonIgnoreIfNull]
    public DateTimeOffset? RequestToSpeakTimestamp { get; set; }
    [BsonIgnoreIfNull]
    public bool? IsPending { get; set; }
    public GuildUserFlags Flags { get; set; }
    [BsonIgnoreIfNull]
    public DateTimeOffset? JoinedAt { get; set; }
    public CacheRole[] Roles { get; set; }
    [BsonIgnoreIfNull]
    public ulong? VoiceChannelId { get; set; }
    [BsonIgnoreIfNull]
    public string? VoiceSessionId { get; set; }
    [BsonIgnoreIfNull]
    public CacheVoiceState? VoiceState { get; set; }
    [BsonIgnoreIfNull]
    public string? GuildBannerHash { get; set; }

    public CacheGuildMemberModel()
    {
        Roles = Array.Empty<CacheRole>();
        GuildPermissions = new();
    }

    public CacheGuildMemberModel Update(SocketGuildUser? user)
    {
        if (user == null)
            return this;
        base.Update(user);
        this.GuildId = user.Guild.Id;
        this.Nickname = user.Nickname;
        this.GuildAvatarId = user.GuildAvatarId;
        this.GuildPermissions = new CacheGuildPermissions(user.GuildPermissions);
        this.IsWebhook = user.IsWebhook;
        this.IsSelfDeafened = user.IsSelfDeafened;
        this.IsSelfMuted = user.IsSelfMuted;
        this.IsSuppressed = user.IsSuppressed;
        this.IsDeafened = user.IsDeafened;
        this.IsMuted = user.IsMuted;
        this.IsStreaming = user.IsStreaming;
        this.IsVideoing = user.IsVideoing;
        this.RequestToSpeakTimestamp = user.RequestToSpeakTimestamp;
        this.IsPending = user.IsPending;
        this.Flags = user.Flags;
        this.JoinedAt = user.JoinedAt;
        this.Roles = user.Roles
            .Select(CacheRole.FromExisting)
            .Where(v => v != null)
            .Cast<CacheRole>()
            .ToArray();
        this.VoiceChannelId = user.VoiceChannel?.Id;
        this.VoiceSessionId = user.VoiceSessionId;
        this.VoiceState = CacheVoiceState.FromExisting(user.VoiceState);
        this.GuildBannerHash = string.IsNullOrEmpty(user.GuildBannerHash) ? null : user.GuildBannerHash;
        return this;
    }
    public static CacheGuildMemberModel? FromExisting(SocketGuildUser? user)
    {
        if (user == null)
            return null;
        return new CacheGuildMemberModel().Update(user);
    }
}