using Discord;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Cache;

public class GuildChannelCacheModel
{
    public const string TableName = "Cache_GuildChannel";
    
    public GuildChannelCacheModel()
    {
        ChannelId = "0";
        GuildId = "0";
        Name = "";
        Kind = GuildChannelCacheKind.Unknown;

        RecordCreatedAt = DateTime.UtcNow;
        RecordUpdatedAt = RecordCreatedAt;
        GuildCache = null!;
    }

    public GuildChannelCacheModel(IGuildChannel channel) : this()
    {
        ChannelId = channel.Id.ToString();
        GuildId = channel.GuildId.ToString();
        Name = string.IsNullOrEmpty(channel.Name?.Trim()) ? "" : channel.Name.Trim();
        Kind = GuildChannelCacheKind.Unknown;

        if (channel is ITextChannel)
            Kind |= GuildChannelCacheKind.Text;
        if (channel is IVoiceChannel)
            Kind |= GuildChannelCacheKind.Voice;
        if (channel is IStageChannel)
            Kind |= GuildChannelCacheKind.Stage;
        if (channel is IThreadChannel)
            Kind |= GuildChannelCacheKind.Thread;
        if (channel is INewsChannel)
            Kind |= GuildChannelCacheKind.News;
        if (channel is ICategoryChannel)
            Kind |= GuildChannelCacheKind.Category;
        if (channel is IForumChannel)
            Kind |= GuildChannelCacheKind.Forum;
        if (channel is IMediaChannel)
            Kind |= GuildChannelCacheKind.Media;
    }

    /// <summary>
    /// Channel Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string ChannelId { get; set; }
    
    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="GuildCacheModel.Id"/>
    /// </remarks>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// Channel Name
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// What kind of channel is this? (flags)
    /// </summary>
    public GuildChannelCacheKind Kind { get; set; }

    /// <summary>
    /// UTC Time when this channel was created
    /// </summary>
    public DateTime? CreatedAt { get; set; }
    /// <summary>
    /// UTC Time when this channel was deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// UTC Time when this record was created
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }
    /// <summary>
    /// UTC Time when this record was updated
    /// </summary>
    public DateTime RecordUpdatedAt { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildCacheModel GuildCache { get; set; }
}