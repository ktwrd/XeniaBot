using Discord;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuditLogActionType = Discord.ActionType;

namespace XeniaDiscord.Data.Models.Cache;

public class BaseAuditLogEntryCacheModel
{
    [JsonConstructor]
    public BaseAuditLogEntryCacheModel()
    {
        Id = "0";
        GuildId = "0";
        PerformedByUserId = "0";
        RecordCreatedAt = DateTime.UtcNow;
        RecordUpdatedAt = RecordCreatedAt;
    }
    public BaseAuditLogEntryCacheModel(IAuditLogEntry entry)
        : this()
    {
        Id = entry.Id.ToString();
        CreatedAt = entry.CreatedAt.UtcDateTime;
        Action = entry.Action;
        PerformedByUserId = entry.User.Id.ToString();
        Reason = string.IsNullOrEmpty(entry.Reason?.Trim()) ? null : entry.Reason.Trim();
        if (entry.Data == null)
        {
            JsonData = null;
        }
        else
        {
            JsonData = JsonSerializer.Serialize(entry.Data, SerializerOptions);
            JsonDataType = entry.Data.GetType().ToString();
        }
    }
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        ReferenceHandler = ReferenceHandler.Preserve,
    };

    /// <summary>
    /// Audit Log Entry Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string Id { get; set; }

    /// <summary>
    /// Guild Id that this Audit Log Entry is from (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <inheritdoc cref="ISnowflakeEntity.CreatedAt"/>
    /// <remarks>
    /// UTC DateTime
    /// </remarks>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc cref="IAuditLogEntry.Action"/>
    public AuditLogActionType Action { get; set; }

    /// <summary>
    /// Discord User Id that performed this Audit Log Action
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string PerformedByUserId { get; set; }

    /// <inheritdoc cref="IAuditLogEntry.Reason"/>
    public string? Reason { get; set; }
    
    /// <summary>
    /// <see cref="IAuditLogEntry.Data"/> serialized into JSON
    /// </summary>
    public string? JsonData { get; set; }

    /// <summary>
    /// Type of the data in <see cref="JsonData"/>
    /// </summary>
    public string? JsonDataType { get; set; }

    public DateTime RecordCreatedAt { get; set; }
    public DateTime RecordUpdatedAt { get; set; }
}
