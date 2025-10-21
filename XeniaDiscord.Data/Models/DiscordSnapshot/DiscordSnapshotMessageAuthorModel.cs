using Discord;

namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotMessageAuthorModel
{
    public const string TableName = "DiscordSnapshotMessageAuthor";
    public Guid Id { get; set; }
    /// <summary>
    /// Foreign Key to <see cref="DiscordSnapshotMessageModel.Id"/>
    /// </summary>
    public Guid SnapshotMessageId { get; set; }
    public string UserId { get; set; } = "0"; // ulong as string
    public bool IsBot { get; set; }
    public bool IsWebhook { get; set; }
    public string Username { get; set; } = "";
    public UserProperties? PublicFlags { get; set; }
    public string? GlobalName { get; set; }
    public DiscordSnapshotMessageAuthorPrimaryGuildModel? PrimaryGuild { get; set; }
}
public class DiscordSnapshotMessageAuthorPrimaryGuildModel
{
    public const string TableName = "DiscordSnapshotMessageAuthorPrimaryGuild";

    public Guid SnapshotMessageAuthorId { get; set; }
    public string? GuildId { get; set; } // ulong as string
    public bool? IdentityEnabled { get; set; }
    public string Tag { get; set; } = "";
    public string BadgeHash { get; set; } = "";
}