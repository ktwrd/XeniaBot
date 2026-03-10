using Discord.WebSocket;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Common.Exceptions;

/// <summary>
/// Exception that is thrown when <see cref="Services.BanSyncService"/>
/// failed to notify a guild of a member being banned.
/// </summary>
public class BanSyncNotifyFailureException : Exception
{
    private readonly string _message;
    public BanSyncNotifyFailureException(
        string message,
        Exception innerException,
        BanSyncRecordModel bansyncRecord,
        BanSyncGuildModel? bansyncGuild,
        SocketGuild? guild,
        SocketGuildUser? guildMember)
        : base(message, innerException)
    {
        _message = message;
        BanSyncRecord = bansyncRecord;
        BanSyncGuild = bansyncGuild;
        Guild = guild;
        GuildMember = guildMember;
    }

    public BanSyncRecordModel BanSyncRecord { get; }
    public BanSyncGuildModel? BanSyncGuild { get; }
    public SocketGuild? Guild { get; }
    public SocketGuildUser? GuildMember { get; }

    public override string Message => string.Join(Environment.NewLine,
        _message,
        $"------ {nameof(BanSyncNotifyFailureException)} Details ------",
        $"BanSyncRecord Id: {BanSyncRecord.Id}",
        $"BanSyncRecord GuildId: {BanSyncRecord.GuildId}",
        $"BanSyncGuild LogChannelId: {BanSyncGuild?.LogChannelId}",
        $"BanSyncGuild Enabled: {BanSyncGuild?.Enable}",
        $"BanSyncGuild State: {BanSyncGuild?.State}",
        "",
        $"Guild Id: {Guild?.Id}",
        $"Guild Name: {Guild?.Name}",
        $"Guild Owner: {Guild?.Owner} ({Guild?.OwnerId})",
        $"Guild Member: {GuildMember} ({GuildMember?.Id})");
}
