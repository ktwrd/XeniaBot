using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageAttachment
{
    public string Filename { get; set; }
    public string Url { get; set; }
    public string ProxyUrl { get; set; }
    public int Size { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }
    public bool Ephemeral { get; set; }
    public string Description { get; set; }
    public string ContentType { get; set; }
    public double? Duration { get; set; }
    public string Waveform { get; set; }
    public AttachmentFlags Flags { get; set; }
    public List<ulong> ClipParticipantIds { get; set; }
    public string Title { get; set; }
    public DateTimeOffset? ClipCreatedAt { get; set; }
    public static CacheMessageAttachment? FromExisting(IAttachment? attachment, CacheMessageAttachment? instance = null)
    {
        if (attachment == null)
            return null;

        instance ??= new CacheMessageAttachment();
        instance.Filename = attachment.Filename;
        instance.Url = attachment.Url;
        instance.ProxyUrl = attachment.ProxyUrl;
        instance.Size = attachment.Size;
        instance.Height = attachment.Height;
        instance.Width = attachment.Width;
        instance.Ephemeral = attachment.Ephemeral;
        instance.Description = attachment.Description;
        instance.ContentType = attachment.ContentType;
        instance.Duration = attachment.Duration;
        instance.Waveform = attachment.Waveform;
        instance.Flags = attachment.Flags;
        instance.ClipParticipantIds = attachment.ClipParticipants.Select(v => v.Id).ToList();
        instance.Title = attachment.Title;
        instance.ClipCreatedAt = attachment.ClipCreatedAt;
        return instance;
    }
}