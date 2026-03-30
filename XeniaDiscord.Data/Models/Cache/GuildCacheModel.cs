using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Cache;

public class GuildCacheModel
{
    public const string TableName = "Cache_Guild";
    public GuildCacheModel()
    {
        Id = "0";
        RecordCreatedAt = DateTime.UtcNow;
        RecordUpdatedAt = RecordCreatedAt;
    }
    public GuildCacheModel(ulong guildId) : this()
    {
        Id = guildId.ToString();
    }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string Id { get; set; }

    /// <summary>
    /// Guild Name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Discord User Id that is the owner of this guild (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? OwnerUserId { get; set; }

    /// <summary>
    /// UTC Time for when this guild was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// UTC Time of when our bot joined this guild. If it's <see langword="null"/>, then we don't know.
    /// // TODO could figure it out based off the Discord System Message that says when a user joined.
    /// </summary>
    public DateTime? JoinedAt { get; set; }

    /// <summary>
    /// URL to the Guild Icon
    /// </summary>
    public string? IconUrl { get; set; }
    /// <summary>
    /// URL to the Banner Image
    /// </summary>
    public string? BannerUrl { get; set; }
    /// <summary>
    /// URL to the Splash Image
    /// </summary>
    public string? SplashUrl { get; set; }
    /// <summary>
    /// URL to the Splash Image used in Discord Discovery
    /// </summary>
    public string? DiscoverySplashUrl { get; set; }

    /// <summary>
    /// UTC Time for when this record was created
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }
    /// <summary>
    /// UTC Time for when this record was updated.
    /// </summary>
    public DateTime RecordUpdatedAt { get; set; }

    public ulong GetGuildId() => Id.ParseRequiredULong(nameof(Id), false);
    public ulong? GetOwnerUserId() => OwnerUserId?.ParseULong(false);

    /// <summary>
    /// Property Accessor
    /// </summary>
    public List<GuildMemberCacheModel> Members { get; set; } = [];
    /// <summary>
    /// Property Accessor
    /// </summary>
    public List<GuildChannelCacheModel> Channels { get; set; } = [];
}
