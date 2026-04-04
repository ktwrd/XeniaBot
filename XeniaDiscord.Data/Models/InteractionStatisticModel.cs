using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models;

public class InteractionStatisticModel
{
    public const string TableName = "Statistics_Interactions";

    public Guid Id { get; set; }

    /// <summary>
    /// Interaction group (first name when there is more than one)
    /// </summary>
    [MaxLength(200)]
    public string? InteractionGroup { get; set; }

    /// <summary>
    /// Interaction name
    /// </summary>
    [MaxLength(200)]
    public string InteractionName { get; set; } = "";

    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; } = "0";

    /// <summary>
    /// Channel Id (ulong as string, optional)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? ChannelId { get; set; }

    /// <summary>
    /// Guild Id (ulong as string, optional)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? GuildId { get; set; }

    /// <summary>
    /// Amount of times the interaction has been used.
    /// </summary>
    public long Count { get; set; }

    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);
    public ulong? GetChannelId() => ChannelId.ParseULong(false);
    public ulong? GetGuildId() => GuildId.ParseULong(false);
}