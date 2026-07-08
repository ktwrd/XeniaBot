using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotEmbedFooterModel
{
    public const string TableName = "Snapshot_Message_Embed_Footer";

    public MessageSnapshotEmbedFooterModel()
    {
        MessageSnapshotEmbedId = Guid.Empty;
        MessageSnapshotEmbed = null!;
    }

    /// <summary>
    /// Primary Key and Foreign Key to <see cref="MessageSnapshotEmbedModel.RecordId"/>
    /// </summary>
    public Guid MessageSnapshotEmbedId { get; set; }

    /// <summary>
    /// <see cref="Discord.EmbedFooter.Text"/>
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// <see cref="Discord.EmbedFooter.IconUrl"/>
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// <see cref="Discord.EmbedFooter.ProxyUrl"/>
    /// </summary>
    public string? ProxyUrl { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotEmbedModel MessageSnapshotEmbed { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotEmbedFooterModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.MessageSnapshotEmbedId);

            b.HasOne(e => e.MessageSnapshotEmbed)
                .WithOne(e => e.Footer)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}