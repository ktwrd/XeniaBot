using Discord;

namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotMessageActivityModel
{
    public const string TableName = "DiscordSnapshotMessageActivity";

    public Guid SnapshotMessageId { get; set; }

    public MessageActivityType Type { get; set; }
    public string? PartyId { get; set; }
}
