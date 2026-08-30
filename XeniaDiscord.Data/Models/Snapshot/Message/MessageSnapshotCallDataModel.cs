using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotCallDataModel
{
    public const string TableName = "Snapshot_Message_CallData";

    public MessageSnapshotCallDataModel()
    {
        RecordId = Guid.NewGuid();
        MessageSnapshotId = Guid.Empty;

        MessageSnapshot = null!;
        Participants = [];
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
    /// UTC Timestamp of when the call ended
    /// </summary>
    public DateTime? CallEndedAt { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel MessageSnapshot { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public List<MessageSnapshotCallDataParticipantModel> Participants { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotCallDataModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.RecordId);

            b.HasOne(e => e.MessageSnapshot)
                .WithOne(e => e.CallData)
                .HasForeignKey<MessageSnapshotCallDataModel>(e => e.MessageSnapshotId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}