namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotMessageEmbedFooterModel
{
    public const string TableName = "DiscordSnapshotMessageEmbedFooter";

    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Foreign Key to <see cref="DiscordSnapshotMessageEmbedModel.Id"/>
    /// </summary>
    public Guid SnapshotMessageEmbedId { get; set; } = Guid.Empty;

    public string? Text { get; set; }
    public string? IconUrl { get; set; }
    public string? ProxyUrl { get; set; }
}
