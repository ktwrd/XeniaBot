using Discord;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotMessageModel
{
    public const string TableName = "DiscordSnapshotMessage";
    public DiscordSnapshotMessageModel()
    {
        Id = Guid.NewGuid();
        SnapshotTimestamp = DateTimeOffset.UtcNow;

        MessageId = "0";
    }
    public Guid Id { get; set; }
    /// <summary>
    /// Time when this snapshot was created
    /// </summary>
    public DateTimeOffset SnapshotTimestamp { get; set; }

    /// <summary>
    /// Discord Message Snowflake (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string MessageId { get; set; }

    /// <summary>
    /// Discord Guild Snowflake (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? GuildId { get; set; }
    /// <summary>
    /// Discord Channel Snowflake (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string ChannelId { get; set; }
    /// <summary>
    /// Discord User Snowflake (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? AuthorId { get; set; }

    public MessageType Type { get; set; }
    public MessageSource Source { get; set; }
    public bool IsTextToSpeech { get; set; }
    public bool IsPinned { get; set; }
    public bool IsSuppressed { get; set; }
    public bool MentionedEveryone { get; set; }
    public string Content { get; set; }
    public string CleanContent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public MessageFlags? Flags { get; set; }

    // Property Accessor
    public DiscordSnapshotMessageAuthorModel Author { get; set; }
    public List<DiscordSnapshotMessageAttachmentModel> Attachments { get; set; }
    public List<DiscordSnapshotMessageEmbedModel> Embeds { get; set; }
    // MentionedChannelIds, MentionedRoleIds, MentionedUserIds
    public DiscordSnapshotMessageActivityModel? Activity { get; set; }
    public DiscordSnapshotMessageApplicationModel? Application { get; set; }
    // public List<DiscordSnapshotMessageReactionModel> Reactions { get; set; }
    // public List<DiscordSnapshotMessageComponentModel> Components { get; set; }
    // public List<DiscordSnapshotMessageSticketModel> Stickers { get; set; }
    // TODO Reactions, Components, Stickers, RoleSubscriptionData, PurchaseNotification, CallData, InteractionMetadata
}
