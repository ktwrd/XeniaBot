using System.ComponentModel.DataAnnotations;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace XeniaDiscord.Data.Models.Snapshot;

// NOTE - Work in progress, not included in XeniaDbContext
public class AttachmentSnapshotModel
{
    public const string TableName = "Snapshot_Attachment";
    public AttachmentSnapshotModel()
    {
        RecordId = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        AttachmentId = "0";
        ClipParticipants = [];
    }

    /// <summary>
    /// Primary Key - Snapshot Record Id
    /// </summary>
    public Guid RecordId { get; set; }
    
    /// <summary>
    /// UTC Date Time of when this record was created at
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }

    /// <summary>
    /// Attachment Id (ulong as string)
    /// <see cref="IAttachment.Id"/>
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string AttachmentId { get; set; }
    
    /// <summary>
    /// UTC Date Time of when this attachment was created at
    /// </summary>
    public DateTime AttachmentCreatedAt { get; set; }
    public string? Filename { get; set; }
    public string? Url { get; set; }
    public string? ProxyUrl { get; set; }
    public long Size { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool Ephemeral { get; set; }
    public string? Description { get; set; }
    public string? ContentType { get; set; }
    public double? AudioDuration { get; set; }
    public string? AudioWaveform { get; set; }
    public string? AudioWaveformBytesAsBase64 { get; set; }
    public AttachmentFlags Flags { get; set; }
    public string? ClipTitle { get; set; }
    public DateTime? ClipCreatedAt { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public List<AttachmentSnapshotClipParticipantModel> ClipParticipants { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttachmentSnapshotModel>(b =>
        {
            b.ToTable(TableName)
                .HasKey(e => e.RecordId);
            b.HasIndex(e => e.RecordCreatedAt).IsUnique(false).IsDescending(true);
        });
    }
}