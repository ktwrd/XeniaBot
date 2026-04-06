using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Data.Models.RolePreserve;

public class RolePreserveUserModel
{
    public const string TableName = "RolePreserveUsers";

    public RolePreserveUserModel()
    {
        GuildId = "0";
        UserId = "";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        GuildMemberSnapshotId = Guid.Empty;
        GuildMemberSnapshot = null!;
        RolePreserveGuild = null!;
    }
    
    /// <summary>
    /// <para>Guild Id (ulong as string)</para>
    /// Also a foreign key to <see cref="RolePreserveGuildModel.GuildId"/>
    /// </summary>
    public string GuildId { get; set; }
    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    public string UserId { get; set; }
    
    /// <summary>
    /// When this record was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// When this record was updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Foreign Key to <see cref="GuildMemberSnapshotModel.RecordId"/>
    /// </summary>
    public Guid GuildMemberSnapshotId { get; set; }
    
    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildMemberSnapshotModel GuildMemberSnapshot { get; set; }
    
    /// <summary>
    /// Property Accessor
    /// </summary>
    public RolePreserveGuildModel RolePreserveGuild { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);
}