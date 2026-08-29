using System.Collections.Generic;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaBot.WebPanel.Areas.BanSync.Models.Search;

public class SearchViewModel
{
    public required BanSyncSearchApiQuery Query { get; init; }
    public AlertComponentViewModel? Alert { get; set; }
    public IReadOnlyCollection<BanSyncRecordModel> Records { get; set; } = [];
    public IReadOnlyCollection<StrippedGuild> AvailableGuilds { get; set; } = [];
    public bool IsSysadmin { get; set; }
}