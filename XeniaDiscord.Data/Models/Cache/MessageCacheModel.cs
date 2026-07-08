using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Data.Models.Cache;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageCacheModel
{
    public const string TableName = "Cache_Message";

    public MessageCacheModel()
    {
        MessageId = "0";
        CreatedAt = DateTimeOffset.UnixEpoch.LocalDateTime;

        MessageSnapshotId = Guid.Empty;
        Snapshot = null!;
    }

    /// <summary>
    /// Primary Key - Message Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string MessageId { get; set; }

    /// <summary>
    /// Discord User Author Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? AuthorId { get; set; }
    
    /// <summary>
    /// Channel Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? ChannelId { get; set; }
    
    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? GuildId { get; set; }

    /// <summary>
    /// Message Content
    /// <see cref="Discord.IMessage.Content"/>
    /// </summary>
    [MaxLength(8000)]
    public string? Content { get; set; }
    /// <summary>
    /// <see cref="Discord.IMessage.CleanContent"/>
    /// </summary>
    [MaxLength(8000)]
    public string? ContentClean { get; set; }

    /// <summary>
    /// UTC Date Time when this message was created at
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// UTC Date Time when this message was edited at
    /// </summary>
    public DateTime? EditedAt { get; set; }
    /// <summary>
    /// UTC Date Time when this message was deleted at
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="MessageSnapshotModel.RecordId"/>
    /// </summary>
    public Guid MessageSnapshotId { get; set; } // this should be updated every time a new snapshot is added
    
    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel Snapshot { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageCacheModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.MessageId);

            b.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.MessageSnapshotId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public ulong GetMessageId() => MessageId.ParseRequiredULong(nameof(MessageId), false);
    public ulong? GetAuthorId() => AuthorId.ParseULong(false);
    public ulong? GetChannelId() => ChannelId.ParseULong(false);
    public ulong? GetGuildId() => GuildId.ParseULong(false);
}