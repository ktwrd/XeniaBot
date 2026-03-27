using Discord;
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
    public UserCacheModel(ulong userId) : this()
    {
        Id = userId.ToString();
    }
    public UserCacheModel(IUser user) : this(user.Id)
    { }

    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string Id { get; set; }

    /// <summary>
    /// UTC Time when this user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; }
    
    /// <summary>
    /// Discriminator. Only used by bots.
    /// </summary>
    public string? Discriminator { get; set; }
    
    /// <summary>
    /// Display name
    /// </summary>
    public string? GlobalName { get; set; }

    /// <summary>
    /// Avatar Url for user.
    /// </summary>
    public string? DisplayAvatarUrl { get; set; }

    /// <summary>
    /// From: <see cref="IUser.IsBot"/>
    /// </summary>
    public bool IsBot { get; set; }

    /// <summary>
    /// From: <see cref="IUser.IsWebhook"/>
    /// </summary>
    public bool IsWebhook { get; set; }

    /// <summary>
    /// UTC Time when this record was first created.
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }
    /// <summary>
    /// UTC Time when this record was last updated.
    /// </summary>
    public DateTime RecordUpdatedAt { get; set; }

    public ulong GetUserId() => Id.ParseRequiredULong(nameof(Id), false);
}
