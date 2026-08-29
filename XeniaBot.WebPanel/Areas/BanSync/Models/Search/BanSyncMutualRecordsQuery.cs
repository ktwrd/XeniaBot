using Microsoft.AspNetCore.Mvc;

namespace XeniaBot.WebPanel.Areas.BanSync.Models.Search;

public class BanSyncMutualRecordsQuery
{
    [FromForm(Name = "page")]
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;
    
    [FromForm(Name = "forUser")]
    [FromQuery(Name = "forUser")]
    public ulong? ForUserId { get; set; }
}
