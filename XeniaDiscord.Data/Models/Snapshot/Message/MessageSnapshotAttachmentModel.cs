using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotAttachmentModel
{
    public const string TableName = "Snapshot_Message_Attachment";
    /// <summary>
    /// Foreign Key to <see cref="MessageSnapshotModel.RecordId"/>
    /// </summary>
    public Guid MessageSnapshotId { get; set; }
    /// <summary>
    /// Foreign Key to <see cref="AttachmentSnapshotModel.RecordId"/>
    /// </summary>
    public Guid AttachmentSnapshotId { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel MessageSnapshot { get; set; } = null!;

    /// <summary>
    /// Property Accessor
    /// </summary>
    public AttachmentSnapshotModel AttachmentSnapshot { get; set; } = null!;

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotAttachmentModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => new
                {
                    e.MessageSnapshotId,
                    e.AttachmentSnapshotId,
                });

            b.HasOne(e => e.AttachmentSnapshot)
                .WithMany()
                .HasForeignKey(e => e.AttachmentSnapshotId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.MessageSnapshot)
                .WithMany(e => e.Attachments)
                .HasForeignKey(e => e.MessageSnapshotId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}