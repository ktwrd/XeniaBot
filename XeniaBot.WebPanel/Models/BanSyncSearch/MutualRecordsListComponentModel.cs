using System;
using System.Collections.Generic;
using System.Linq;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaBot.WebPanel.Models.BanSyncSearch;

public class MutualRecordsListComponentModel
{
    public ICollection<BanSyncRecordModel> Items { get; set; } = [];
    public IEnumerable<RowModel> Rows => Items
        .Select(e => new RowModel
        {
            Record = e,
            Page = Page,
            GuildId = GuildId,
            IsLast = IsItemLastInList(e.Id)
        });
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public required ulong GuildId { get; set; }

    public long CurrentGuildCount { get; set; }
    public long OtherGuildCount { get; set; }
    public long TotalCount { get; set; }
    public bool IsItemLastInList(Guid id)
    {
        if (Items.Count < PageSize) return true;
        return Items.Last().Id == id;
    }
}
