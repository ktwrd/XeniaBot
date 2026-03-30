using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.GuildApproval;

public class GuildApprovalLogEventModel
{
    public const string TableName = "GuildApprovalLogEvent";

    public GuildApprovalLogEventModel()
    {
        Id = Guid.NewGuid();
        GuildId = "0";
        UserId = "0";
        ApprovedByUserId = "0";
        RecordCreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Record Id (primary key)
    /// </summary>
    public Guid Id { get; set; }

    public string GuildId { get; set; }

    /// <summary>
    /// Id of the User that was approved (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }

    /// <summary>
    /// User Id that approved <see cref="UserId"/> (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string ApprovedByUserId { get; set; }

    /// <summary>
    /// When this record was created (UTC)
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);
    public ulong GetApprovedByUserId() => ApprovedByUserId.ParseRequiredULong(nameof(ApprovedByUserId), false);
}