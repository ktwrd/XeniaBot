namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotMessageEmbedVideoModel
{
    public const string TableName = "DiscordSnapshotMessageEmbedVideo";

    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Foreign Key to <see cref="DiscordSnapshotMessageEmbedModel.Id"/>
    /// </summary>
    public Guid SnapshotMessageEmbedId { get; set; } = Guid.Empty;

    public string Url { get; set; } = "";
    public int? Height { get; set; }
    public int? Width { get; set; }
}
