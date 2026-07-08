using Discord;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotModel
{
    public const string TableName = "Snapshot_Message";

    public MessageSnapshotModel()
    {
        RecordId = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        MessageId = "0";
        
        MentionedChannels = [];
        MentionedRoles = [];
        MentionedUsers = [];
        Attachments = [];
        Reactions = [];
        Stickers = [];
        Embeds = [];
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid RecordId { get; set; }

    /// <summary>
    /// UTC Time of when this snapshot was created
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }

    /// <summary>
    /// Message Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)] public string MessageId { get; set; }
    /// <summary>
    /// (optional) Author User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)] public string? AuthorUserId { get; set; }
    /// <summary>
    /// (optional) Channel Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)] public string? ChannelId { get; set; }
    /// <summary>
    /// (optional) Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)] public string? GuildId { get; set; }
    /// <summary>
    /// (optional) Thread Channel Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)] public string? ThreadChannelId { get; set; }

    /// <summary>
    /// UTC Time of when this message was created
    /// </summary>
    public DateTime MessageCreatedAt { get; set; }
    /// <summary>
    /// UTC Time of when this message was edited
    /// </summary>
    public DateTime? MessageEditedAt { get; set; }
    /// <summary>
    /// UTC Time of when this message was deleted
    /// </summary>
    public DateTime? MessageDeletedAt { get; set; }

    public DiscordMessageTypeDto MessageType { get; set; }
    public DiscordMessageSourceDto MessageSource { get; set; }

    /// <summary>
    /// <see cref="IMessage.IsTTS"/>
    /// </summary>
    public bool IsTTS { get; set; }
    /// <summary>
    /// <see cref="IMessage.IsPinned"/>
    /// </summary>
    public bool IsPinned { get; set; }
    /// <summary>
    /// <see cref="IMessage.IsSuppressed"/>
    /// </summary>
    public bool IsSuppressed { get; set; }
    /// <summary>
    /// <see cref="IMessage.MentionedEveryone"/>
    /// </summary>
    public bool MentionedEveryone { get; set; }
    
    /// <summary>
    /// <see cref="IMessage.Content"/>
    /// </summary>
    [MaxLength(8000)]
    public string? Content { get; set; }
    /// <summary>
    /// <see cref="IMessage.CleanContent"/>
    /// </summary>
    [MaxLength(8000)]
    public string? ContentClean { get; set; }

    /// <summary>
    /// <see cref="IMessage.Flags"/>
    /// </summary>
    public MessageFlags? MessageFlags { get; set; }

    #region Property Accessors
    public List<MessageSnapshotMentionedChannelModel> MentionedChannels { get; set; }
    public List<MessageSnapshotMentionedRoleModel> MentionedRoles { get; set; }
    public List<MessageSnapshotMentionedUserModel> MentionedUsers { get; set; }
    public List<MessageSnapshotAttachmentModel> Attachments { get; set; }
    public List<MessageSnapshotReactionModel> Reactions { get; set; }
    public List<MessageSnapshotStickerModel> Stickers { get; set; }
    public List<MessageSnapshotEmbedModel> Embeds { get; set; }
    
    public MessageSnapshotReferenceModel? Reference { get; set; }
    public MessageSnapshotActivityModel? Activity { get; set; }
    public MessageSnapshotApplicationModel? Application { get; set; }
    public MessageSnapshotRoleSubscriptionDataModel? RoleSubscriptionData { get; set; }
    public MessageSnapshotPurchaseNotificationModel? PurchaseNotification { get; set; }
    public MessageSnapshotCallDataModel? CallData { get; set; }
    #endregion

    // TODO IMessageComponent[]
    // TODO IEmbed[]
    // TODO ITag[] (maybe!)

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.RecordId);

            b.HasIndex(e => new
            {
                e.MessageId,
                e.RecordCreatedAt
            }).IsDescending().IsUnique(false);

            b.Property(e => e.MessageType).HasConversion<string>();
            b.Property(e => e.MessageSource).HasConversion<string>();
        });
    }

    public ulong GetMessageId() => MessageId.ParseRequiredULong(nameof(MessageId), false);
    public ulong? GetAuthorUserId() => AuthorUserId.ParseULong(false);
    public ulong? GetChannelId() => ChannelId.ParseULong(false);
    public ulong? GetGuildId() => GuildId.ParseULong(false);
    public ulong? GetThreadChannelId() => ThreadChannelId.ParseULong(false);
}


// TODO make mapper for Discord.MessageActivityType
public enum DiscordMessageActivityTypeDto
{
    Join,
    Spectate,
    Listen,
    JoinRequest,
}

// TODO make mapper for Discord.PurchaseType
public enum DiscordPurchaseTypeDto
{
    GuildProduct
}