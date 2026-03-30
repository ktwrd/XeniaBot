using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.GuildApproval;

/// <summary>
/// Model for configuring the (member) Approval system in a guild.
/// </summary>
public class GuildApprovalModel
{
    public const string TableName = "GuildApproval";

    public GuildApprovalModel()
    {
        GuildId = "0";
    }

    /// <summary>
    /// Guild Id (ulong as string, primary key)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// Discord Role Id that's given to approved users
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? ApprovedRoleId { get; set; }

    /// <summary>
    /// Discord Role Id that's given to users who can always approve a user.
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? ApproverRoleId { get; set; }

    /// <summary>
    /// Log Channel for approvals (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? LogChannelId { get; set; }

    /// <summary>
    /// Is the approval system enabled?
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Should approved users be greeted in a separate channel?
    /// </summary>
    public bool EnableGreeter { get; set; }
    /// <summary>
    /// Channel Id to greet new user in (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? GreeterChannelId { get; set; }
    /// <summary>
    /// Message template for greeter
    /// </summary>
    [MaxLength(5000)]
    public string? GreeterMessageTemplate { get; set; }
    /// <summary>
    /// Should the greeter template be in an embed?
    /// </summary>
    public bool GreeterAsEmbed { get; set; }
    /// <summary>
    /// Should the new user be mentioned in the content?
    /// </summary>
    /// <remarks>
    /// If <see cref="GreeterMessageTemplate"/> contains <c>{user_mention}</c>, and it's not in an embed, then the user won't be mentioned at the beginning of the message.
    /// </remarks>
    public bool GreeterMentionUser { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong? GetApprovedRoleId() => ApprovedRoleId.ParseULong(false);
    public ulong? GetApproverRoleId() => ApproverRoleId.ParseULong(false);
    public ulong? GetLogChannelId() => LogChannelId.ParseULong(false);
    public ulong? GetGreeterChannelId() => GreeterChannelId.ParseULong(false);
}