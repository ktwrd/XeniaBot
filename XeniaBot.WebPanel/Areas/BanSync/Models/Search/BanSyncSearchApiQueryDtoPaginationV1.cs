using System;
using System.Text.Json.Serialization;

namespace XeniaBot.WebPanel.Areas.BanSync.Models.Search;

public class BanSyncSearchApiQueryDtoPaginationV1
{
    [JsonPropertyName("page")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Page { get; set; }
    
    [JsonPropertyName("limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PageSize { get; set; }
    
    public int GetPage()
        => Math.Min(1, Page ?? 1);

    public int GetPageSize()
        => Math.Clamp(PageSize ?? 20, 10, 100);

    public BanSyncSearchApiQueryPagination ToRecord()
    {
        return new BanSyncSearchApiQueryPagination
        {
            Page = GetPage(),
            Limit = GetPageSize()
        };
    }
}