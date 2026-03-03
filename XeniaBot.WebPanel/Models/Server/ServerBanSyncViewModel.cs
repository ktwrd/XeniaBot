using Discord.WebSocket;
using XeniaBot.MongoData.Models;

namespace XeniaBot.WebPanel.Models;

public class ServerBanSyncViewModel
    : BaseViewModel
    , IBanSyncBaseRecords
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    public long BanSyncRecordCount { get; set; }
    public ulong? FilterRecordsByUserId { get; set; }
    public BanSyncStateHistoryItemModel BanSyncGuild { get; set; }
}

public interface IBanSyncBaseRecords
{
    public long BanSyncRecordCount { get; set; }
    public ulong? FilterRecordsByUserId { get; set; }
    public BanSyncStateHistoryItemModel BanSyncGuild { get; set; }
}