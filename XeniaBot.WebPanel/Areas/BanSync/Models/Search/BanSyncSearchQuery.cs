using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace XeniaBot.WebPanel.Areas.BanSync.Models.Search;

public class BanSyncSearchQuery
{
    [FromForm(Name = "guildId")]
    public List<ulong>? GuildIdFilter { get; set; }

    [FromForm(Name = "userId")]
    public List<ulong>? FilterUserIds { get; set; }
    
    [FromForm(Name = "createdBy")]
    public List<ulong>? FilterCreatedByUserIds { get; set; }
    
    [FromForm(Name = "sortBy")]
    public BanSyncSearchApiQuerySortBy? SortBy { get; set; }
    
    [FromForm(Name = "sortDirection")]
    public BanSyncSearchApiQuerySortDirection? SortDirection { get; set; }

    [FromForm(Name = "requestedBy")]
    public ulong RequestingGuildId { get; set; }
    
    [FromForm(Name = "includeRequestor")]
    public bool? IncludeRequestingGuild { get; set; }
    
    [FromForm(Name = "inclGhost")]
    public bool IncludeGhostRecords { get; set; }
    
    [FromForm(Name = "ghost")]
    public bool GhostState { get; set; }
    
    [FromForm(Name = "page")]
    public int Page
    {
        get;
        set => field = Math.Max(value, 1);
    } = 1;

    [FromForm(Name = "pageSize")]
    public int PageSize
    {
        get;
        set => field = Math.Clamp(value, 10, 50);
    } = PageSizeDefault;

    internal const int PageSizeDefault = 20;
    internal const int PageSizeMin = 10;
    internal const int PageSizeMax = 50;
}
