using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageAttachment
{
    public string AttachmentId { get; set; } = "0";
    public DateTimeOffset AttachmentCreatedAt { get; set; }
    
    public string Filename { get; set; }
    public string Url { get; set; }
    public string ProxyUrl { get; set; }
    public int Size { get; set; }
    [BsonIgnoreIfNull]
    public int? Height { get; set; }
    [BsonIgnoreIfNull]
    public int? Width { get; set; }
    public bool Ephemeral { get; set; }
    [BsonIgnoreIfNull]
    public string? Description { get; set; }
    [BsonIgnoreIfNull]
    public string? ContentType { get; set; }
    [BsonIgnoreIfNull]
    public double? Duration { get; set; }
    [BsonIgnoreIfNull]
    public string? Waveform { get; set; }
    [BsonIgnoreIfNull]
    public byte[]? WaveformBytes { get; set; }
    public AttachmentFlags Flags { get; set; }
    [BsonIgnoreIfNull]
    public string[]? ClipParticipantUserIds { get; set; }
    [BsonIgnoreIfNull]
    public string? Title { get; set; }
    [BsonIgnoreIfNull]
    public DateTimeOffset? ClipCreatedAt { get; set; }

    public CacheMessageAttachment Update(IAttachment other)
    {
        AttachmentId = other.Id.ToString();
        AttachmentCreatedAt = other.CreatedAt;

        Filename = other.Filename;
        Url = other.Url;
        ProxyUrl = other.ProxyUrl;
        Size = other.Size;
        Height = other.Height;
        Width = other.Width;
        Ephemeral = other.Ephemeral;
        Description = other.Description;
        ContentType = other.ContentType;
        Duration = other.Duration;
        Waveform = other.Waveform;
        WaveformBytes = other.WaveformBytes;
        Flags = other.Flags;
        ClipParticipantUserIds = other.ClipParticipants == null
            ? null
            : other.ClipParticipants.Select(e => e.Id.ToString()).ToArray();
        Title = other.Title;
        ClipCreatedAt = other.ClipCreatedAt;
        return this;
    }

    public static CacheMessageAttachment? FromExisting(IAttachment? other)
    {
        if (other == null)
            return null;

        return new CacheMessageAttachment().Update(other);
    }
}