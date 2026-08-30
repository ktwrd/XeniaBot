using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class AttachmentSnapshotClipParticipantModel
{
    public const string TableName = "Snapshot_Attachment_ClipParticipant";

    /// <summary>
    /// Foreign Key to <see cref="AttachmentSnapshotModel.RecordId"/>
    /// </summary>
    public Guid AttachmentSnapshotId { get; set; }
    
    /// <summary>
    /// Attachment Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string AttachmentId { get; set; } = "0";
    
    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; } = "0";

    /// <summary>
    /// Property Accessor
    /// </summary>
    public AttachmentSnapshotModel? AttachmentSnapshot { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttachmentSnapshotClipParticipantModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => new
                {
                    e.AttachmentSnapshotId,
                    e.UserId
                });
            b.HasIndex(e => new
            {
                e.AttachmentSnapshotId,
                e.AttachmentId,
                e.UserId
            }).IsUnique(false).IsDescending(true);

            b.HasOne(e => e.AttachmentSnapshot)
                .WithMany(e => e.ClipParticipants)
                .HasForeignKey(e => e.AttachmentSnapshotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}