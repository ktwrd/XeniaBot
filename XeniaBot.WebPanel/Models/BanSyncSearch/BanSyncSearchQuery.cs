using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace XeniaBot.WebPanel.Models.BanSyncSearch;

public class BanSyncSearchQuery
{
    [FromForm(Name = "guildId")]
    public List<ulong>? GuildIdFilter { get; set; }

    [FromForm(Name = "userId")]
    public List<ulong>? UserIdFilter { get; set; }

    [FromForm(Name = "reason")]
    public string? Reason { get; set; }

    [FromForm(Name = "page")]
    public int Page { get; set; } = 1;
}
