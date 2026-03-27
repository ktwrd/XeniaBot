using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.PartialSnapshot;

public class UserPartialSnapshotModel
{
    public const string TableName = "UserPartialSnapshot";
    public UserPartialSnapshotModel()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        UserId = "0";
        Username = "";
        DisplayName = "";
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// UTC Time when this record was created.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }

    /// <summary>
    /// Username (from <see cref="Discord.IUser.Username"/>)
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Discriminator (from <see cref="Discord.IUser.Discriminator"/>, but it's <see langword="null"/> when <see cref="string.IsNullOrEmpty(string?)"/> or zero)
    /// </summary>
    [MaxLength(8)]
    public string? Discriminator { get; set; }

    /// <summary>
    /// Global/Display Name (from <see cref="Discord.IUser.GlobalName"/>)
    /// </summary>
    public string DisplayName { get; set; }

    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);

    public string FormatUsername() => string.IsNullOrEmpty(Discriminator?.Trim()) ? Username : $"{Username}#{Discriminator}";
}
