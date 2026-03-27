using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.PartialSnapshot;

public class GuildPartialSnapshotModel
{
    public const string TableName = "GuildPartialSnapshot";

    public GuildPartialSnapshotModel()
    {
        Id = Guid.NewGuid();
        GuildId = "0";
        Name = "";
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// From <see cref="Discord.IGuild.Name"/>
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// UTC Time when this partial snapshot was created
    /// </summary>
    public DateTime Timestamp { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
}
