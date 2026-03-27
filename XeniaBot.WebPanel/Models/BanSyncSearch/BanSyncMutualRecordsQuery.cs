using Microsoft.AspNetCore.Mvc;

namespace XeniaBot.WebPanel.Models.BanSyncSearch;

public class BanSyncMutualRecordsQuery
{
    [FromForm(Name = "page")]
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;
}
