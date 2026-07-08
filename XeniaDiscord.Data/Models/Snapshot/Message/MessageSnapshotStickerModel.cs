using System.ComponentModel.DataAnnotations;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotStickerModel
{
    public const string TableName = "Snapshot_Message_Sticker";

    public MessageSnapshotStickerModel()
    {
        RecordId = Guid.NewGuid();
        MessageSnapshotId = Guid.Empty;
        StickerId = "0";
        Name = string.Empty;

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
    /// Sticker Id (ulong as string)
    /// <see cref="IStickerItem.Id"/>
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string StickerId { get; set; }

    /// <summary>
    /// <see cref="IStickerItem.Name"/>
    /// </summary>
    [MaxLength(100)] // TODO figure out actual max length
    public string Name { get; set; }

    /// <summary>
    /// <see cref="IStickerItem.Id"/>
    /// </summary>
    public StickerFormatType Format { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel MessageSnapshot { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotStickerModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.RecordId);

            b.HasOne(e => e.MessageSnapshot)
                .WithMany(e => e.Stickers)
                .HasForeignKey(e => e.MessageSnapshotId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}