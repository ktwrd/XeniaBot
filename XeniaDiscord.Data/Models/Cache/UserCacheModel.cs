using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Cache;

public class UserCacheModel
{
    public const string TableName = "Cache_User";

    public UserCacheModel()
    {
        Id = "0";
        Username = "";
        CreatedAt = DateTime.UtcNow;
        RecordCreatedAt = DateTime.UtcNow;
        RecordUpdatedAt = RecordCreatedAt;
    }

    [MaxLength(DbGlobals.ulongMaxLength)]
    public string Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Username { get; set; }
    public string? Discriminator { get; set; }
    public string? GlobalName { get; set; }

    public string? DisplayAvatarUrl { get; set; }

    public DateTime RecordCreatedAt { get; set; }
    public DateTime RecordUpdatedAt { get; set; }

    public ulong GetUserId() => Id.ParseRequiredULong(nameof(Id), false);
}
