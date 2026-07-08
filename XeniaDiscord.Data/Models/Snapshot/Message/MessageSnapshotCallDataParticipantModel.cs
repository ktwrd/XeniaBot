using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotCallDataParticipantModel
{
    public const string TableName = "Snapshot_Message_CallData_Participant";

    public MessageSnapshotCallDataParticipantModel()
    {
        MessageSnapshotCallDataId = Guid.Empty;
        UserId = "0";
        MessageSnapshotCallData = null!;
    }

    /// <summary>
    /// Foreign Key to <see cref="MessageSnapshotCallDataModel.RecordId"/>
    /// </summary>
    public Guid MessageSnapshotCallDataId { get; set; }

    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotCallDataModel MessageSnapshotCallData { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotCallDataParticipantModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => new
                {
                    e.MessageSnapshotCallDataId,
                    e.UserId
                });

            b.HasOne(e => e.MessageSnapshotCallData)
                .WithMany(e => e.Participants)
                .HasForeignKey(e => e.MessageSnapshotCallDataId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}