using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Data.Models.ServerLog;

public class ServerLogGuildModel
{
    public const string TableName = "ServerLogGuilds";

    public ServerLogGuildModel()
    {
        GuildId = "0";
        Enabled = true;

        ServerLogChannels = [];
    }
    public ServerLogGuildModel(ulong guildId) : this()
    {
        GuildId = guildId.ToString();
    }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    /// <remarks>
    /// This value is also the primary key
    /// </remarks>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// Is server logging enabled for this guild?
    /// </summary>
    /// <remarks>
    /// This should not prevent snapshots/events from being captured.
    /// This is only to prevent notifications from being sent in server logging channels.
    /// </remarks>
    [DefaultValue(true)]
    public bool Enabled { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildCacheModel? GuildCache { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public List<ServerLogChannelModel> ServerLogChannels { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
}