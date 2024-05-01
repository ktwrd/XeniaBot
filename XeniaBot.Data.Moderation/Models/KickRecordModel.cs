using MongoDB.Bson.Serialization.Attributes;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Moderation.Models;

public class KickRecordModel : BaseModelGuid
{
    public static string CollectionName => "mod_kickRecord";
    
    /// <summary>
    /// <para><b>Stored as ulong</b></para>
    ///
    /// <inheritdoc cref="GetGuildId()"/>
    /// </summary>
    public string GuildId { get; set; }

    /// <summary>
    /// <para>What guild the user was kicked in.</para>
    /// </summary>
    public ulong GetGuildId()
    {
        return ulong.Parse(GuildId);
    }
    
    /// <summary>
    /// <para><b>Stored as ulong</b></para>
    ///
    /// <inheritdoc cref="GetUserId()"/>
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// <para>User Id of who was kicked.</para>
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
    /// <para>Who kicked <see cref="UserId"/></para>
    /// </summary>
    public ulong? GetActionedByUserId()
    {
        return ActionedByUserId == null ? null : ulong.Parse(ActionedByUserId);
    }
    
    /// <summary>
    /// Reason why the member was kicked.
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// <para>Unix Timestamp (UTC, <b>Seconds</b>)</para>
    ///
    /// <para>When this record was created.</para>
    /// </summary>
    public long CreatedAt { get; set; }

    public KickRecordModel()
        : base()
    {
        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        GuildId = "0";
        UserId = "0";
    }
}