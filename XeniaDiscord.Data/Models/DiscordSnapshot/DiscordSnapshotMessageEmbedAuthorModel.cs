namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotMessageEmbedAuthorModel
{
    public const string TableName = "DiscordSnapshotMessageEmbedAuthor";

    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Foreign Key to <see cref="DiscordSnapshotMessageEmbedModel.Id"/>
    /// </summary>
    public Guid SnapshotMessageEmbedId { get; set; } = Guid.Empty;

    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? IconUrl { get; set; }
    public string? ProxyIconUrl { get; set; }
}
