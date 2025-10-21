using Discord;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotMessageAttachmentModel
{
    public const string TableName = "DiscordSnapshotMessageAttachment";
    public DiscordSnapshotMessageAttachmentModel()
    {
        Id = Guid.NewGuid();
        SnapshotMessageId = Guid.Empty;
        SnapshotTimestamp = DateTimeOffset.UtcNow;
        AttachmentId = "0";
        Filename = "";
        Url = "";
        ProxyUrl = "";
    }


    public Guid Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="DiscordSnapshotMessageModel.Id"/>
    /// </summary>
    public Guid SnapshotMessageId { get; set; }

    /// <summary>
    /// Time when this snapshot was created
    /// </summary>
    public DateTimeOffset SnapshotTimestamp { get; set; }

    /// <summary>
    /// Discord Attachment Snowflake (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string AttachmentId { get; set; }
    public string Filename { get; set; }
    public string Url { get; set; }
    public string ProxyUrl { get; set; }
    public int Size { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }
    public bool Epheremal { get; set; }
    public string? Description { get; set; }
    public string? ContentType { get; set; }
    public double? Duration { get; set; }
    public string? Waveform { get; set; }
    public AttachmentFlags Flags { get; set; }
    public string? Title { get; set; }
    public DateTimeOffset? ClipCreatedAt { get; set; }
}
