using System.Collections.Generic;
using System.Linq;
using Discord;
using XeniaBot.Data;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models.Component;

public class BanSyncMutualRecordsListComponentViewModel : BaseViewModel,
    IBanSyncBaseRecordsComponent
{
    #region IBanSyncBaseRecordsComponent
    /// <inheritdoc />
    public IEnumerable<BanSyncInfoModel> Items { get; set; }
    /// <inheritdoc />
    public int Cursor { get; set; }
    #endregion
    
    /// <summary>
    /// Will be <see langword="true"/> when either of the following conditions are met;
    /// <list type="bullet">
    /// <item>The count of <see cref="Items"/> is less than <see cref="PageSize"/></item>
    /// <item><see cref="AbsolutePageEndIndex"/> is greater than (or equal) to <see cref="BanSyncRecordCount"/></item>
    /// </list>
    /// </summary>
    public bool IsLastPage => Items.Count() < PageSize || AbsolutePageEndIndex >= BanSyncRecordCount;
    public ListViewStyle ListStyle { get; set; }
    public const int PageSize = 10;
    /// <summary>
    /// Current guild configuration for the BanSync module.
    /// </summary>
    public BanSyncStateHistoryItemModel BanSyncGuild { get; set; }
    /// <summary>
    /// All records that are considered a "mutual" record.
    /// </summary>
    public long BanSyncRecordCount { get; set; }
    /// <summary>
    /// Filter by a specific user id.
    /// </summary>
    public ulong? FilterRecordsByUserId { get; set; } 
    /// <summary>
    /// Guild that is checking for mutual records.
    /// </summary>
    public IGuild Guild { get; set; }
    /// <summary>
    /// Requesting user.
    /// </summary>
    public IGuildUser User { get; set; }
    /// <summary>
    /// Amount of records before this page.
    /// </summary>
    public int AbsoluteStartPageIndex => PageSize * Cursor;
    /// <summary>
    /// Amount of records in this page, and every page before this one.
    /// </summary>
    public int AbsolutePageEndIndex => Items.Count() + AbsoluteStartPageIndex;

    public bool IsItemLast(BanSyncInfoModel item)
    {
        if (Items.Count() < PageSize)
            return true;

        return Items.ElementAt(Items.Count() - 1).RecordId == item.RecordId;
    }
}

public interface IBanSyncBaseRecordsComponent : IBanSyncBaseRecords
{
    /// <summary>
    /// Items to display for this section for this page.
    /// </summary>
    public IEnumerable<BanSyncInfoModel> Items { get; set; }
    /// <summary>
    /// Page index, starting from zero.
    /// </summary>
    public int Cursor { get; set; }
}