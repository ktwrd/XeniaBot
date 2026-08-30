using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotMentionedRoleModel
{
    public const string TableName = "Snapshot_Message_MentionedRole";

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
    /// Role Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string RoleId { get; set; } = "0";

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel MessageSnapshot { get; set; } = null!;

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotMentionedRoleModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => new
                {
                    e.MessageSnapshotId,
                    e.RoleId
                });
            b.HasIndex(e => new
            {
                e.MessageId,
                e.RoleId
            }).IsUnique(false).IsDescending(true);

            b.HasOne(e => e.MessageSnapshot)
                .WithMany(e => e.MentionedRoles)
                .HasForeignKey(e => e.MessageSnapshotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}