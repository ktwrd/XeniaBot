using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class MessageSnapshotEmbedFieldModel
{
    public const string TableName = "Snapshot_Message_Embed_Field";

    public MessageSnapshotEmbedFieldModel()
    {
        RecordId = Guid.NewGuid();
        MessageSnapshotEmbedId = Guid.Empty;
        RecordSortOrder = 0;
        Name = string.Empty;
        Value = string.Empty;

        MessageSnapshotEmbed = null!;
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid RecordId { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="MessageSnapshotEmbedModel.RecordId"/>
    /// </summary>
    public Guid MessageSnapshotEmbedId { get; set; }

    // NOTE should increment every time a new embed is added to a message, so it's properly sorted in the web ui
    public int RecordSortOrder { get; set; }

    /// <summary>
    /// <see cref="Discord.EmbedField.Name"/>
    /// </summary>
    [MaxLength(100)] // TODO figure out actual max length
    public string Name { get; set; }

    /// <summary>
    /// <see cref="Discord.EmbedField.Value"/>
    /// </summary>
    [MaxLength(4000)]
    public string Value { get; set; }

    /// <summary>
    /// <see cref="Discord.EmbedField.Inline"/>
    /// </summary>
    public bool Inline { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public MessageSnapshotEmbedModel MessageSnapshotEmbed { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageSnapshotEmbedFieldModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.RecordId);

            b.HasIndex(e => new
            {
                e.MessageSnapshotEmbedId,
                e.RecordSortOrder
            }).IsUnique(false).IsDescending(false);

            b.HasOne(e => e.MessageSnapshotEmbed)
                .WithMany(e => e.Fields)
                .HasForeignKey(e => e.MessageSnapshotEmbedId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}