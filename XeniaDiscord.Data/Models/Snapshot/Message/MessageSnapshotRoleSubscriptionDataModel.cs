using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotRoleSubscriptionDataModel
{
    public const string TableName = "Snapshot_Message_RoleSubscriptionData";
    public MessageSnapshotRoleSubscriptionDataModel()
    {
        RecordId = Guid.NewGuid();
        MessageSnapshotId = Guid.Empty;
        SkuId = "0";
        TierName = string.Empty;

        MessageSnapshot = null!;
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
    /// Subscription Sku Id (ulong as string)
    /// <see cref="Discord.MessageRoleSubscriptionData.Id"/>
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string SkuId { get; set; }
    
    /// <summary>
    /// <see cref="Discord.MessageRoleSubscriptionData.TierName"/>
    /// </summary>
    public string TierName { get; set; } // TODO figure out max length
    
    /// <summary>
    /// <see cref="Discord.MessageRoleSubscriptionData.MonthsSubscribed"/>
    /// </summary>
    public int MonthsSubscribed { get; set; }
    
    /// <summary>
    /// <see cref="Discord.MessageRoleSubscriptionData.IsRenewal"/>
    /// </summary>
    public bool IsRenewal { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotModel MessageSnapshot { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotRoleSubscriptionDataModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.RecordId);

            b.HasOne(e => e.MessageSnapshot)
                .WithOne(e => e.RoleSubscriptionData)
                .HasForeignKey<MessageSnapshotRoleSubscriptionDataModel>(e => e.MessageSnapshotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}