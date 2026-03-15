using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Cache;

public class AuditLogBanCacheModel : BaseAuditLogEntryCacheModel
{
    public const string TableName = "Cache_AuditLogEntry_Ban";

    public AuditLogBanCacheModel() : base()
    {
    }
    public AuditLogBanCacheModel(IAuditLogEntry entry)
        : base(entry)
    {
        if (entry.Data is BanAuditLogData data)
        {
            TargetUserId = data.Target?.Id.ToString();
        }
        else if (entry.Data is SocketBanAuditLogData socketData)
        {
            if (socketData.Target.HasValue)
            {
                TargetUserId = socketData.Target.Value.Id.ToString();
            }
            try
            {
                TargetUserId = socketData.Target.Id.ToString();
            }
            catch { }
        }
        else if (entry.Data != null)
        {
            throw new ArgumentException(
                $"{nameof(entry)}.{nameof(entry.Data)}",
                $"Invalid type for {nameof(entry.Data)}: {entry.Data?.GetType()}");
        }
    }

    /// <summary>
    /// <see langword="null"/> when the user account is deleted. <see cref="BanAuditLogData.Target"/>
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? TargetUserId { get; set; }
}
