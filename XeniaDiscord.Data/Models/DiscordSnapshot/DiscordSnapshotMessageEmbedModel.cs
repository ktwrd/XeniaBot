using Discord;

namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotMessageEmbedModel
{
    public const string TableName = "DiscordSnapshotMessageEmbed";
    public DiscordSnapshotMessageEmbedModel()
    {
        Id = Guid.NewGuid();
        SnapshotMessageId = Guid.Empty;
    }

    public Guid Id { get; set; }
    /// <summary>
    /// Foreign Key to <see cref="DiscordSnapshotMessageModel.Id"/>
    /// </summary>
    public Guid SnapshotMessageId { get; set; }

    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public EmbedType? Type { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public string? ColorHex { get; set; } // Discord.Color.ToString()
    
    public DiscordSnapshotMessageEmbedImageModel? Image { get; set; }
    public DiscordSnapshotMessageEmbedVideoModel? Video { get; set; }
    public DiscordSnapshotMessageEmbedAuthorModel? Author { get; set; }
    public DiscordSnapshotMessageEmbedFooterModel? Footer { get; set; }
    public DiscordSnapshotMessageEmbedProviderModel? Provider { get; set; }
    public DiscordSnapshotMessageEmbedThumbnailModel? Thumbnail { get; set; }
    public List<DiscordSnapshotMessageEmbedFieldModel> Fields { get; set; }
}
