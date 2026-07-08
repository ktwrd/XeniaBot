using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotPurchaseNotificationModel
{
    public const string TableName = "Snapshot_Message_PurchaseNotification";

    public MessageSnapshotPurchaseNotificationModel()
    {
        RecordId = Guid.NewGuid();
        MessageSnapshotId = Guid.Empty;
        GuildProductPurchaseSnapshotId = Guid.Empty;

        MessageSnapshot = null!;
        GuildProductPurchaseSnapshot = null!;
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
    /// <see cref="Discord.PurchaseNotification.Type"/>
    /// </summary>
    public DiscordPurchaseTypeDto Type { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="GuildProductPurchaseSnapshotModel.RecordId"/>
    /// </summary>
    public Guid GuildProductPurchaseSnapshotId { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel MessageSnapshot { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildProductPurchaseSnapshotModel GuildProductPurchaseSnapshot { get; set; }

    public static void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<MessageSnapshotPurchaseNotificationModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.RecordId);

            b.Property(e => e.Type).HasConversion<string>();

            b.HasOne(e => e.MessageSnapshot)
                .WithOne(e => e.PurchaseNotification)
                .HasForeignKey<MessageSnapshotActivityModel>(e => e.MessageSnapshotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.GuildProductPurchaseSnapshot)
                .WithMany()
                .HasForeignKey(e => e.GuildProductPurchaseSnapshotId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}