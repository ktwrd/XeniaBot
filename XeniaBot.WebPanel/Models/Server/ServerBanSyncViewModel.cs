using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public class ServerBanSyncViewModel : BaseViewModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    public List<BanSyncInfoModel> BanSyncRecords { get; set; }
    public ulong? FilterRecordsByUserId { get; set; }
    public BanSyncStateHistoryItemModel BanSyncGuild { get; set; }
    
    public int Cursor { get; set; }
    public bool IsLastPage => BanSyncRecords.Count < PageSize;
    public const int PageSize = 10;

    public bool IsItemLast(BanSyncInfoModel model)
    {
        if (BanSyncRecords.Count < 2)
            return true;

        return BanSyncRecords.Last().RecordId == model.RecordId;
    }
}