using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models;

public class UserPartialSnapshotModel
{
    public const string TableName = "UserPartialSnapshot";
    public UserPartialSnapshotModel()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UserId = "0";
        Username = "";
        DisplayName = "";
    }

    public Guid Id { get; set; }
    /// <summary>
    /// UTC Time when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }
    public string Username { get; set; }
    [MaxLength(8)]
    public string? Discriminator { get; set; }
    public string DisplayName { get; set; }

    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);
}
