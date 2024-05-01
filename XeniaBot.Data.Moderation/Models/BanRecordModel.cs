using MongoDB.Bson.Serialization.Attributes;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Moderation.Models;

public class BanRecordModel : BaseModelGuid
{
    public static string CollectionName => "mod_banRecord";
    
    /// <summary>
    /// <para><b>Stored as ulong</b></para>
    /// 
    /// <inheritdoc cref="GetGuildId()"/>
    /// </summary>
    public string GuildId { get; set; }

    /// <summary>
    /// <para>Guild Id this Ban Record belongs to.</para>
    /// </summary>
    public ulong GetGuildId()
    {
        return ulong.Parse(GuildId);
    }
    
    /// <summary>
    /// <para>Unix Timestamp (UTC, <b>Seconds</b>)</para>
    /// 
    /// <para>Timestamp when this record was initially added to the database.</para>
    /// </summary>
    public long Timestamp { get; set; }
    
    /// <summary>
    /// <para>Unix Timestamp (UTC, <b>Seconds</b>)</para>
    /// 
    /// <para>Timestamp when this ban record was created. Will use <see cref="Timestamp"/> if it was fetched from the <see cref="Discord.IGuild.GetBansAsync"/>.</para>
    /// </summary>
    public long CreatedAt { get; set; }
    /// <summary>
    /// <para><b>Stored as ulong</b></para>
    /// 
    /// <inheritdoc cref="GetUserId()"/>
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// <para>User that was banned.</para>
    /// </summary>
    public ulong GetUserId()
    {
        return ulong.Parse(UserId);
    }
    /// <summary>
    /// <para><b>Stored as ulong</b></para>
    ///
    /// <inheritdoc cref="GetActionedByUserId()"/>
    /// </summary>
    public string? ActionedByUserId { get; set; }

    /// <summary>
    /// User who banned <see cref="UserId"/>. Will be `null` if we don't know.
    /// </summary>
    public ulong? GetActionedByUserId()
    {
        return ActionedByUserId == null ? null : ulong.Parse(ActionedByUserId);
    }

    /// <summary>
    /// Ban Reason.
    /// </summary>
    public string? Reason { get; set; }

    public BanRecordModel()
        : base()
    {
        UserId = "0";
        GuildId = "0";
    }
}