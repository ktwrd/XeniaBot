using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotApplicationModel
{
    public const string TableName = "Snapsnot_Message_Application";

    public MessageSnapshotApplicationModel()
    {
        MessageApplicationRecordId = Guid.NewGuid();
        MessageSnapshotId = Guid.Empty;
        ApplicationId = "0";
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid MessageApplicationRecordId { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="MessageSnapshotModel.RecordId"/>
    /// </summary>
    public Guid MessageSnapshotId { get; set; }

    /// <summary>
    /// Application Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string ApplicationId { get; set; }

    public string? CoverImageId { get; set; }
    public string? Description { get; set; }
    public string? IconId { get; set; }
    public string? IconUrl { get; set; }
    public string? Name { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel MessageSnapshot { get; set; } = null!;

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotApplicationModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.MessageApplicationRecordId);

            b.HasOne(e => e.MessageSnapshot)
                .WithOne(e => e.Application)
                .HasForeignKey<MessageSnapshotApplicationModel>(e => e.MessageSnapshotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}