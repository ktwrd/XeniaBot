using Discord.WebSocket;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaBot.WebPanel.Models;

public class ServerBanSyncViewModel
    : BaseViewModel
    , IBanSyncBaseRecords
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    public long BanSyncRecordCount { get; set; }
    public ulong? FilterRecordsByUserId { get; set; }
    public BanSyncGuildSnapshotModel BanSyncGuild { get; set; }
}

public interface IBanSyncBaseRecords
{
    public long BanSyncRecordCount { get; set; }
    public ulong? FilterRecordsByUserId { get; set; }
    public BanSyncGuildSnapshotModel BanSyncGuild { get; set; }
}