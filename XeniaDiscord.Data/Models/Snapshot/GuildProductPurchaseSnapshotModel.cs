using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class GuildProductPurchaseSnapshotModel
{
    public const string TableName = "Snapshot_GuildProductPurchase";
    public GuildProductPurchaseSnapshotModel()
    {
        RecordId = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        ListingId = "0";
        ProductName = string.Empty;
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid RecordId { get; set; }
    
    /// <summary>
    /// UTC Date of when this record was created
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }

    /// <summary>
    /// <see cref="Discord.GuildProductPurchase.ListingId"/>
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string ListingId { get; set; }

    /// <summary>
    /// <see cref="Discord.GuildProductPurchase.ProductName"/>
    /// </summary>
    [MaxLength(200)] // TODO figure out what the actual max length is
    public string ProductName { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuildProductPurchaseSnapshotModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.RecordId);
            b.HasIndex(e => e.RecordCreatedAt)
                .IsDescending().IsUnique(false);
        });
    }
}