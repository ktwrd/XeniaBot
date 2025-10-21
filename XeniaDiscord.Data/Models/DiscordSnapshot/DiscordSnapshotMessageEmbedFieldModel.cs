namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotMessageEmbedFieldModel
{
    public const string TableName = "DiscordSnapshotMessageEmbedField";

    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Foreign Key to <see cref="DiscordSnapshotMessageEmbedModel.Id"/>
    /// </summary>
    public Guid SnapshotMessageEmbedId { get; set; } = Guid.Empty;

    public string? Name { get; set; }
    public string? Value { get; set; }
    public bool Inline { get; set; }
}
