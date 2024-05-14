using System.Collections.Generic;
using System.Linq;
using Discord;
using XeniaBot.Data;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models.Component;

public class BanSyncMutualRecordsListComponentViewModel : BaseViewModel,
    IBanSyncBaseRecordsComponent
{
    public IEnumerable<BanSyncInfoModel> Items { get; set; }
    public int Cursor { get; set; }
    public bool IsLastPage => Items.Count() < PageSize;
    public ListViewStyle ListStyle { get; set; }
    public const int PageSize = 10;
    public BanSyncStateHistoryItemModel BanSyncGuild { get; set; }
    public long BanSyncRecordCount { get; set; }
    public ulong? FilterRecordsByUserId { get; set; } 
    public IGuild Guild { get; set; }
    public IGuildUser User { get; set; }

    public bool IsItemLast(BanSyncInfoModel item)
    {
        if (Items.Count() < 2)
            return true;

        return Items.ElementAt(Items.Count() - 1).RecordId == item.RecordId;
    }
}

public interface IBanSyncBaseRecordsComponent : IBanSyncBaseRecords
{
    public IEnumerable<BanSyncInfoModel> Items { get; set; }
    public int Cursor { get; set; }
}