using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotMentionedChannelModel
{
    public const string TableName = "Snapshot_Message_MentionedChannel";

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
    /// Channel Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string ChannelId { get; set; } = "0";

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel? MessageSnapshot { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotMentionedChannelModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => new
                {
                    e.MessageSnapshotId,
                    e.ChannelId
                });
            b.HasIndex(e => new
            {
                e.MessageId,
                e.ChannelId
            }).IsUnique(false).IsDescending(true);

            b.HasOne(e => e.MessageSnapshot)
                .WithMany(e => e.MentionedChannels)
                .HasForeignKey(e => e.MessageSnapshotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}