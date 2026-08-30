using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotReactionModel
{
    public const string TableName = "Snapshot_Message_Reaction";

    public MessageSnapshotReactionModel()
    {
        RecordId = Guid.NewGuid();
        MessageSnapshotId = Guid.Empty;
        MessageId = "0";
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

    /// <summary>
    /// Message Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string MessageId { get; set; }

    /// <summary>
    /// <see cref="Discord.IEmote.Name"/>
    /// </summary>
    public string EmoteName { get; set; } = string.Empty;
    /// <summary>
    /// (optional) <see cref="Discord.Emote.Id"/> (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? EmoteId { get; set; }
    /// <summary>
    /// (optional) <see cref="Discord.Emote.Animated"/>
    /// </summary>
    public bool? EmoteIsAnimated { get; set; }
    /// <summary>
    /// (optional) <see cref="Discord.Emote.Url"/>
    /// </summary>
    [MaxLength(1000)]
    public string? EmoteUrl { get; set; }

    /// <summary>
    /// <see cref="Discord.ReactionMetadata.ReactionCount"/>
    /// </summary>
    public int ReactionCount { get; set; }
    /// <summary>
    /// <see cref="Discord.ReactionMetadata.IsMe"/>
    /// </summary>
    public bool IsMe { get; set; }
    /// <summary>
    /// <see cref="Discord.ReactionMetadata.BurstCount"/>
    /// </summary>
    public int BurstCount { get; set; }
    /// <summary>
    /// <see cref="Discord.ReactionMetadata.NormalCount"/>
    /// </summary>
    public int NormalCount { get; set; }

    // TODO Add ReactionMetadata.BurstColors at some point

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel MessageSnapshot { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotReactionModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.RecordId);

            b.HasIndex(e => new
            {
                e.MessageSnapshotId,
                e.EmoteName,
                e.EmoteId
            }).IsUnique(false).IsDescending();

            b.HasOne(e => e.MessageSnapshot)
                .WithMany()
                .HasForeignKey(e => e.MessageSnapshotId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
