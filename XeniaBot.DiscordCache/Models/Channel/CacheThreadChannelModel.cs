using Discord;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheThreadChannelModel : CacheTextChannelModel
{
    public ThreadType Type { get; set; }
    public ulong OwnerId { get; set; }
    public bool HasJoined { get; set; }
    public bool IsPrivateThread { get; set; }
    public ulong ParentChannelId { get; set; }
    public int MessageCount { get; set; }
    public int MemberCount { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset ArchiveTimestamp { get; set; }
    public ThreadArchiveDuration AutoArchiveDuration { get; set; }
    public bool IsLocked { get; set; }
    public bool? IsInvitable { get; set; }
    public ulong[] AppliedTags { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ulong[] UserIds { get; set; }

    public new CacheThreadChannelModel Update(SocketThreadChannel channel)
    {
        base.Update(channel);
        Type = channel.Type;
        OwnerId = channel.Owner.Id;
        HasJoined = channel.HasJoined;
        IsPrivateThread = channel.IsPrivateThread;
        ParentChannelId = channel.ParentChannel.Id;
        MessageCount = channel.MessageCount;
        MemberCount = channel.MemberCount;
        IsArchived = channel.IsArchived;
        ArchiveTimestamp = channel.ArchiveTimestamp;
        AutoArchiveDuration = channel.AutoArchiveDuration;
        IsLocked = channel.IsLocked;
        IsInvitable = channel.IsInvitable;
        AppliedTags = channel.AppliedTags.ToArray();
        CreatedAt = channel.CreatedAt;
        UserIds = channel.Users.Select(v => v.Id).ToArray();
        return this;
    }

    public static CacheThreadChannelModel? FromExisting(SocketThreadChannel? channel)
    {
        if (channel == null)
            return null;

        var instance = new CacheThreadChannelModel();
        return instance.Update(channel);
    }
}