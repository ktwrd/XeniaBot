using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotEmbedModel
{
    public const string TableName = "Snapshot_Message_Embed";

    public MessageSnapshotEmbedModel()
    {
        RecordId = Guid.NewGuid();
        MessageSnapshotId = Guid.Empty;
        RecordSortOrder = 0;
        MessageSnapshot = null!;
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid RecordId { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="MessageSnapshotModel.RecordId"/>
    /// </summary>
    public Guid MessageSnapshotId { get; set; }

    // NOTE should increment every time a new embed is added to a message, so it's properly sorted in the web ui
    public int RecordSortOrder { get; set; }

    [MaxLength(1000)] // TODO figure out actual max length
    public string? Url { get; set; }
    [MaxLength(1000)] // TODO figure out actual max length
    public string? Title { get; set; }
    [MaxLength(8000)] // TODO figure out actual max length
    public string? Description { get; set; }

    /// <summary>
    /// <see cref="Discord.IEmbed.Type"/> as string
    /// </summary>
    public DiscordEmbedTypeDto Type { get; set; } // TODO use DiscordEmbedTypeDto

    /// <summary>
    /// <see cref="Discord.IEmbed.Timestamp"/> in UTC
    /// </summary>
    public DateTime? Timestamp { get; set; }

    // public MessageSnapshotEmbedColorModel? Color { get; set; }
    // public MessageSnapshotEmbedImageModel? Image { get; set; }
    // public MessageSnapshotEmbedVideoModel? Video { get; set; }
    // public MessageSnapshotEmbedAuthorModel? Author { get; set; }
    public MessageSnapshotEmbedFooterModel? Footer { get; set; }
    // public MessageSnapshotEmbedProviderModel? Provider { get; set; }
    // public MessageSnapshotEmbedThumbnailModel? Thumbnail { get; set; }
    public List<MessageSnapshotEmbedFieldModel> Fields { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel MessageSnapshot { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotEmbedModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.RecordId);

            b.Property(e => e.Type).HasConversion<string?>();

            b.HasIndex(e => new
            {
                e.MessageSnapshotId,
                e.RecordSortOrder
            }).IsUnique(false).IsDescending(false);
        });
    }
}

// TODO create mapper from Discord.EmbedType
public enum DiscordEmbedTypeDto
{
    Unknown,
    Rich,
    Link,
    Video,
    Image,
    Gifv,
    Article,
    Tweet,
    Html,
}