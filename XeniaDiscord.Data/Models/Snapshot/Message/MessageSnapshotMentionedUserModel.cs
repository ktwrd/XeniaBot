using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotMentionedUserModel
{
    public const string TableName = "Snapshot_Message_MentionedUser";

    /// <summary>
    /// Foreign Key to <see cref="MessageSnapshotModel.RecordId"/>
    /// </summary>
    public Guid MessageSnapshotId { get; set; }

    /// <summary>
    /// Message Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string MessageId { get; set; } = "0";

    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; } = "0";

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel? MessageSnapshot { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotMentionedUserModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => new
                {
                    e.MessageSnapshotId,
                    e.UserId
                });
            b.HasIndex(e => new
            {
                e.MessageId,
                e.UserId
            }).IsUnique(false).IsDescending(true);

            b.HasOne(e => e.MessageSnapshot)
                .WithMany(e => e.MentionedUsers)
                .HasForeignKey(e => e.MessageSnapshotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}