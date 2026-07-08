using System.ComponentModel.DataAnnotations;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotReferenceModel
{
    public const string TableName = "Snapshot_Message_MessageReference";
    public MessageSnapshotReferenceModel()
    {
        MessageSnapshotReferenceId = Guid.NewGuid();
        MessageSnapshotId = Guid.Empty;
        MessageSnapshotCreatedAt = DateTimeOffset.UnixEpoch.UtcDateTime;
        MessageId = "0";
    }
    
    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid MessageSnapshotReferenceId { get; set; }
    
    /// <summary>
    /// Foreign Key to <see cref="MessageSnapshotModel.RecordId"/>
    /// </summary>
    public Guid MessageSnapshotId { get; set; }
    
    /// <summary>
    /// UTC Date Time of when the <see cref="MessageSnapshot"/> was created.
    /// </summary>
    public DateTime MessageSnapshotCreatedAt { get; set; }
    
    /// <summary>
    /// Message Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string MessageId { get; set; }

    /// <summary>
    /// (optional) Message Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? ReferencedMessageId { get; set; }
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? ReferencedGuildId { get; set; }
    [MaxLength(DbGlobals.ulongMaxLength)]
    public bool? FailIfNotExists { get; set; }
    public MessageReferenceType? ReferenceType { get; set; }

    public MessageSnapshotModel MessageSnapshot { get; set; } = null!;

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotReferenceModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.MessageSnapshotReferenceId);

            b.Property(e => e.ReferenceType).HasConversion<string?>();

            b.HasIndex(e => new
            {
                e.MessageSnapshotId,
                e.MessageSnapshotCreatedAt,
                e.MessageId,
            }).IsUnique(false).IsDescending();

            b.HasOne(e => e.MessageSnapshot)
                .WithOne(e => e.Reference)
                .HasForeignKey<MessageSnapshotReferenceModel>(e => e.MessageSnapshotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}