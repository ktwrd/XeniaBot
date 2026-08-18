namespace XeniaBot.WebPanel.Models.BanSyncSearch;

public class BanSyncSearchApiQueryPagination
{
    /// <summary>
    /// 1-based
    /// </summary>
    public required int Page { get; init; }
    public required int Limit { get; init; }
}