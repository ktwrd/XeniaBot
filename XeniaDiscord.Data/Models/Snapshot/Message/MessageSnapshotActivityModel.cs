using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotActivityModel
{
    public const string TableName = "Snapshot_Message_Activity";

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid MessageSnapshotActivityId { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="MessageSnapshotModel.RecordId"/>
    /// </summary>
    public Guid MessageSnapshotId { get; set; }

    /// <summary>
    /// <see cref="Discord.MessageActivity.Type"/>
    /// </summary>
    public DiscordMessageActivityTypeDto Type { get; set; }

    /// <summary>
    /// <see cref="Discord.MessageActivity.PartyId"/>
    /// </summary>
    [MaxLength(2000)]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel MessageSnapshot { get; set; } = null!;

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotActivityModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.MessageSnapshotActivityId);

            b.Property(e => e.Type).HasConversion<string>();

            b.HasOne(e => e.MessageSnapshot)
                .WithOne(e => e.Activity)
                .HasForeignKey<MessageSnapshotActivityModel>(e => e.MessageSnapshotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}