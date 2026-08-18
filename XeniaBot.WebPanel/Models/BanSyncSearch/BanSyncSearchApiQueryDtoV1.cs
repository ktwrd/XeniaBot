using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using XeniaDiscord.Data;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace XeniaBot.WebPanel.Models.BanSyncSearch;

public class BanSyncSearchApiQueryDtoV1
{
    /// <summary>
    /// (optional) Filter by User Ids that were banned
    /// </summary>
    [JsonPropertyName("userId")]
    public string[]? FilterUserIds { get; set; }
    
    /// <summary>
    /// (optional) Filter by User Ids that banned people
    /// </summary>
    [JsonPropertyName("createdByUserId")]
    public string[]? FilterCreatedByUserIds { get; set; }
 
    /// <summary>
    /// (optional) Filter by Guild Ids
    /// </summary>
    [JsonPropertyName("guildIds")]
    public string[]? FilterGuildIds { get; set; }
    
    /// <summary>
    /// (optional) Unix Timestamp as seconds
    /// </summary>
    [JsonPropertyName("filterBeforeTs")]
    public string? FilterCreatedAtBefore { get; set; }
    
    /// <summary>
    /// (optional) Unix Timestamp as seconds
    /// </summary>
    [JsonPropertyName("filterAfterTs")]
    public string? FilterCreatedAtAfter { get; set; }

    /// <summary>
    /// (optional) Sort by, will use Created At by default
    /// </summary>
    [JsonPropertyName("sortBy")]
    public BanSyncSearchApiQuerySortBy? SortBy { get; set; }
    
    /// <summary>
    /// (optional) Sort direction, like asc or desc
    /// </summary>
    [JsonPropertyName("sortDirection")]
    public BanSyncSearchApiQuerySortDirection? SortDirection { get; set; }
    
    /// <summary>
    /// Guild ID that is requesting the search
    /// </summary>
    [JsonPropertyName("reqGuildId")]
    public string? RequestingGuildId { get; set; }
    
    [JsonPropertyName("includeRequestingGuildRecords")]
    public bool? IncludeRequestingGuildRecords { get; set; }
    
    /// <summary>
    /// Page navigation
    /// </summary>
    [JsonPropertyName("pagination")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BanSyncSearchApiQueryDtoPaginationV1? Pagination { get; set; }

    #region Get/Parser Methods
    public ulong[] GetFilterUserIds()
        => ParseSnowflakeArray(FilterUserIds);
    
    public ulong[] GetFilterCreatedByUserIds()
        => ParseSnowflakeArray(FilterCreatedByUserIds);
    
    public ulong[] GetFilterGuildIds()
        => ParseSnowflakeArray(FilterGuildIds);
    
    public Maybe<DateTime> GetFilterCreatedAtBefore()
        => ParseStringToDateTimeUtc(FilterCreatedAtBefore);
    
    public Maybe<DateTime> GetFilterCreatedAtAfter()
        => ParseStringToDateTimeUtc(FilterCreatedAtAfter);

    public ulong? GetRequestingGuildId()
        => RequestingGuildId.ParseULong(allowZero: false);
    
    public BanSyncSearchApiQuerySortBy GetSortBy()
        => SortBy.GetValueOrDefault(BanSyncSearchApiQuerySortBy.CreatedAt);
    
    public BanSyncSearchApiQuerySortDirection GetSortDirection()
        => SortDirection.GetValueOrDefault(BanSyncSearchApiQuerySortDirection.Descending);

    public BanSyncSearchApiQueryDtoPaginationV1 GetPagination()
        => Pagination ?? new BanSyncSearchApiQueryDtoPaginationV1();
    
    private static Maybe<DateTime> ParseStringToDateTimeUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Maybe.None;
        if (long.TryParse(value, out var ts) &&
            ts > BanSyncSearchApiQuery.FilterEpoch)
        {
            return DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
        }
        const DateTimeStyles styles = DateTimeStyles.AssumeUniversal
                                      | DateTimeStyles.AllowInnerWhite
                                      | DateTimeStyles.AllowLeadingWhite
                                      | DateTimeStyles.AllowTrailingWhite
                                      | DateTimeStyles.AllowWhiteSpaces;
        if (DateTimeOffset.TryParse(value, DateTimeFormatInfo.InvariantInfo, styles, out var dt))
        {
            return dt.UtcDateTime;
        }

        return Maybe.None;
    }
    
    private static ulong[] ParseSnowflakeArray(string?[]? values)
    {
        return values?.Select(static e =>
        {
            if (string.IsNullOrWhiteSpace(e)) return (ulong)0;
            if (ulong.TryParse(e, out var v) && v > 0)
                return v;
            return (ulong)0;
        }).Where(e => e > 0).Distinct().ToArray() ?? [];
    }
    #endregion

    public BanSyncSearchApiQuery ToRecord()
    {
        return new BanSyncSearchApiQuery
        {
            FilterUserIds = GetFilterUserIds(),
            FilterCreatedByUserIds = GetFilterCreatedByUserIds(),
            FilterGuildIds = GetFilterGuildIds(),
            FilterCreatedAtBefore = GetFilterCreatedAtBefore(),
            FilterCreatedAtAfter = GetFilterCreatedAtAfter(),
            SortBy = GetSortBy(),
            SortDirection = GetSortDirection(),
            RequestingGuildId = GetRequestingGuildId() ?? 0,
            EnforceGuildVisibility = true,
            IncludeRequestingGuildRecordsInResult = IncludeRequestingGuildRecords ?? true,
            GhostState = false,
            Pagination = GetPagination().ToRecord()
        };
    }
}