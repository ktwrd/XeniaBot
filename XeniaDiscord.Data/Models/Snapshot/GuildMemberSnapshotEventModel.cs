
namespace XeniaDiscord.Data.Models.Snapshot;

/// <summary>
/// Used to define what event caused a Guild Snapshot, including some things that might've changed.
/// </summary>
public class GuildMemberSnapshotEventModel
{
    public const string TableName = "SnapshotEvent_GuildMember";

    public GuildMemberSnapshotEventModel()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        GuildId = "0";
        UserId = "0";
        Source = default;
        BeforeId = null;
        AfterId = Guid.Empty;
        Before = null;
        Current = null!;
    }

    /// <summary>
    /// Record Id (primary key)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// UTC Time of when this record was created
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    public string GuildId { get; set; }
    
    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Event Source
    /// </summary>
    public DiscordSnapshotSource Source { get; set; }

    /// <summary>
    /// (optional) Foreign Key to <see cref="GuildMemberSnapshotModel.RecordId"/>
    /// </summary>
    public Guid? BeforeId { get; set; }
    
    /// <summary>
    /// Foreign Key to <see cref="GuildMemberSnapshotModel.RecordId"/>
    /// </summary>
    public Guid AfterId { get; set; }

    /// <summary>
    /// (partial) descriptor of what changed before & after the user got updated.
    /// </summary>
    public GuildMemberSnapshotEventWhatChanged WhatChanged { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildMemberSnapshotModel? Before { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildMemberSnapshotModel Current { get; set; }
}

[Flags]
public enum GuildMemberSnapshotEventWhatChanged : ulong
{
    /// <summary>
    /// <see cref="GuildMemberSnapshotModel.Username"/>
    /// <see cref="GuildMemberSnapshotModel.Discriminator"/>
    /// </summary>
    Username = 1 << 0,
    /// <summary>
    /// <see cref="GuildMemberSnapshotModel.Nickname"/>
    /// </summary>
    Nickname = 1 << 1,
    /// <summary>
    /// <see cref="GuildMemberSnapshotModel.IsSelfDeafened"/>
    /// <see cref="GuildMemberSnapshotModel.IsSelfMuted"/>
    /// <see cref="GuildMemberSnapshotModel.IsSuppressed"/>
    /// <see cref="GuildMemberSnapshotModel.IsDeafened"/>
    /// <see cref="GuildMemberSnapshotModel.IsMuted"/>
    /// <see cref="GuildMemberSnapshotModel.IsStreaming"/>
    /// <see cref="GuildMemberSnapshotModel.VoiceChannelId"/>
    /// </summary>
    VoiceStatus = 1 << 2,
    /// <summary>
    /// <see cref="GuildMemberSnapshotModel.GuildAvatarId"/>
    /// <see cref="GuildMemberSnapshotModel.AvatarUrl"/>
    /// </summary>
    Avatar = 1 << 3,
    Roles = 1 << 4,
    Permissions = 1 << 5,
    /// <summary>
    /// <see cref="GuildMemberSnapshotModel.TimedOutUntil"/>
    /// <see cref="GuildMemberSnapshotModel.IsPending"/>
    /// </summary>
    Moderation = 1 << 6
}