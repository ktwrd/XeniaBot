using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public class ServerBanSyncViewModel : BaseViewModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    public List<BanSyncInfoModel> BanSyncRecords { get; set; }
    public ulong? FilterRecordsByUserId { get; set; }
}