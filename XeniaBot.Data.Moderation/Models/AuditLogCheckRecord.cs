using MongoDB.Bson.Serialization.Attributes;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Moderation.Models;

public class AuditLogCheckRecord : BaseModelGuid
{
    public static string CollectionName => "mod_auditLogCheck";

    /// <summary>
    /// <para><b>Stored as ulong</b></para>
    ///
    /// <inheritdoc cref="GetGuildId()"/>
    /// </summary>
    public string GuildId { get; set; }

    /// <summary>
    /// Guild Id this record is for.
    /// </summary>
    public ulong GetGuildId()
    {
        return ulong.Parse(GuildId);
    }
    
    /// <summary>
    /// Audit Log Action this is referring to.
    /// </summary>
    public AuditLogActionType ActionType { get; set; }

    public enum AuditLogActionType
    {
        Kick
    }
    /// <summary>
    /// <para>Unix Timestamp (UTC, <b>Seconds</b>)</para>
    /// 
    /// <para>When this was last updated.</para>
    /// </summary>
    public long Timestamp { get; set; }
    /// <summary>
    /// <para>Unix Timestamp (UTC, <b>Seconds</b>)</para>
    ///
    /// <para>When this record was first inserted</para>
    /// </summary>
    public long InsertTimestamp { get; set; }
    /// <summary>
    /// Previous AuditLog Id that this is associated with
    /// </summary>
    public ulong? LastId { get; set; }
    /// <summary>
    /// Value of <see cref="XeniaBot.Shared.Services.CoreContext.InstanceId"/> that this was created by.
    /// </summary>
    public string InstanceId { get; set; }

    public AuditLogCheckRecord()
        : base()
    {
        
    }

}