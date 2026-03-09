using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Cache;

public class GuildCacheModel
{
    public const string TableName = "Cache_Guild";

    [MaxLength(DbGlobals.ulongMaxLength)]
    public string Id { get; set; } = "0";

    public string? Name { get; set; }

    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? OwnerUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? JoinedAt { get; set; }

    public ulong GetGuildId() => Id.ParseRequiredULong(nameof(Id), false);
    public ulong? GetOwnerUserId() => OwnerUserId?.ParseULong(false);

    public DateTime RecordCreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime RecordUpdatedAt { get; set; } = DateTime.UtcNow;

    public List<GuildMemberCacheModel> Members { get; set; } = [];
}
