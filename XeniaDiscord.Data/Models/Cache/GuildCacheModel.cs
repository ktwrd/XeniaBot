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

    [MaxLength(DbGlobals.ulongMaxLength)]
    public string Id { get; set; }

    public string? Name { get; set; }

    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? OwnerUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? JoinedAt { get; set; }

    public string? IconUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? SplashUrl { get; set; }
    public string? DiscoverySplashUrl { get; set; }

    public DateTime RecordCreatedAt { get; set; }
    public DateTime RecordUpdatedAt { get; set; }

    public ulong GetGuildId() => Id.ParseRequiredULong(nameof(Id), false);
    public ulong? GetOwnerUserId() => OwnerUserId?.ParseULong(false);

    public List<GuildMemberCacheModel> Members { get; set; } = [];
}
