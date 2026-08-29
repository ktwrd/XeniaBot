using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using XeniaDiscord.Data.Repositories;
using SearchQuerySortBy = XeniaDiscord.Data.Repositories.BanSyncRecordRepository.SearchQuerySortBy;
using SearchQuerySortDirection = XeniaDiscord.Data.Repositories.BanSyncRecordRepository.SearchQuerySortDirection;
// ReSharper disable RedundantSwitchExpressionArms

namespace XeniaBot.WebPanel.Areas.BanSync.Models.Search;

public class BanSyncSearchApiQuery
{
    /// <summary>
    /// 2015-01-01 00:00:00 UTC
    /// </summary>
    public const int FilterEpoch = 1420070400;

    [JsonIgnore]
    public Maybe<ulong[]> FilterUserIds { get; init; } = Maybe.None;
    [JsonIgnore]
    public Maybe<ulong[]> FilterCreatedByUserIds { get; init; } = Maybe.None;
    [JsonIgnore]
    public Maybe<ulong[]> FilterGuildIds { get; init; } = Maybe.None;
    [JsonIgnore]
    public Maybe<DateTime> FilterCreatedAtBefore { get; init; } = Maybe.None;
    [JsonIgnore]
    public Maybe<DateTime> FilterCreatedAtAfter { get; init; } = Maybe.None;
    public required BanSyncSearchApiQuerySortBy SortBy { get; init; }
    public required BanSyncSearchApiQuerySortDirection SortDirection { get; init; }
    /// <summary>
    /// This can only be <c>0</c> or None if the requestor is a developer
    /// </summary>
    [JsonIgnore]
    public Maybe<ulong> RequestingGuildId { get; init; }
    public required bool IncludeRequestingGuildRecordsInResult { get; init; }
    public required bool EnforceGuildVisibility { get; set; }
    /// <summary>
    /// null = don't check
    /// true = Ghost must be True
    /// false = Ghost must be False
    /// </summary>
    public bool? GhostState { get; init; }
    public required BanSyncSearchApiQueryPagination Pagination { get; init; }
    
    #region debug shit because C# Functional Extensions doesn't have json serializing/deserializing for Maybe<T>
    [JsonPropertyName(nameof(FilterUserIds))]
    public ulong[]? JsonFilterUserIds => FilterUserIds.HasValue ? FilterUserIds.Value : null;
    [JsonPropertyName(nameof(FilterCreatedByUserIds))]
    public ulong[]? JsonFilterCreatedByUserIds => FilterCreatedByUserIds.HasValue ? FilterCreatedByUserIds.Value : null;
    [JsonPropertyName(nameof(FilterGuildIds))]
    public ulong[]? JsonFilterGuildIds => FilterGuildIds.HasValue ? FilterGuildIds.Value : null;
    [JsonPropertyName(nameof(FilterCreatedAtBefore))]
    public MaybeRecord<DateTime> JsonFilterCreatedAtBefore => new(FilterCreatedAtBefore.GetValueOrDefault(DateTime.MinValue), FilterCreatedAtBefore.HasValue);
    [JsonPropertyName(nameof(FilterCreatedAtAfter))]
    public MaybeRecord<DateTime> JsonFilterCreatedAtAfter => new(FilterCreatedAtAfter.GetValueOrDefault(DateTime.MinValue), FilterCreatedAtAfter.HasValue);
    [JsonPropertyName(nameof(RequestingGuildId))]
    public MaybeRecord<ulong> JsonRequestingGuildId => new (RequestingGuildId.GetValueOrDefault(0), RequestingGuildId.HasValue);
    #endregion

    public BanSyncSearchQuery ToQueryForm()
    {
        return new BanSyncSearchQuery
        {
            FilterUserIds = FilterUserIds.HasValue ? [.. FilterUserIds.Value] : null,
            FilterCreatedByUserIds = FilterCreatedByUserIds.HasValue ? [.. FilterCreatedByUserIds.Value] : null,
            GuildIdFilter = FilterGuildIds.HasValue ? [.. FilterGuildIds.Value] : null,
            SortBy = SortBy,
            SortDirection = SortDirection,
            RequestingGuildId = RequestingGuildId.HasValue ? RequestingGuildId.Value : 0,
            IncludeRequestingGuild = IncludeRequestingGuildRecordsInResult,
            IncludeGhostRecords = GhostState == null,
            GhostState = GhostState.GetValueOrDefault(false),
            Page = Pagination.Page,
            PageSize = Pagination.Limit,
        };
    }
    
    public BanSyncSearchApiQueryDtoV1 ToQueryDto()
    {
        var dto = new BanSyncSearchApiQueryDtoV1()
        {
            SortBy = SortBy,
            SortDirection = SortDirection,
            IncludeRequestingGuildRecords = IncludeRequestingGuildRecordsInResult,
            Pagination = new BanSyncSearchApiQueryDtoPaginationV1()
            {
                Page = Pagination.Page,
                PageSize = Pagination.Limit
            }
        };
        if (FilterUserIds.HasValue)
        {
            dto.FilterUserIds =
            [ 
                .. FilterUserIds.Value
                    .Distinct().Select(e => e.ToString("D", CultureInfo.InvariantCulture))
            ];
        }
        if (FilterCreatedByUserIds.HasValue)
        {
            dto.FilterCreatedByUserIds =
            [ 
                .. FilterCreatedByUserIds.Value
                    .Distinct().Select(e => e.ToString("D", CultureInfo.InvariantCulture))
            ];
        }
        if (FilterGuildIds.HasValue)
        {
            dto.FilterGuildIds =
            [ 
                .. FilterGuildIds.Value
                    .Distinct().Select(e => e.ToString("D", CultureInfo.InvariantCulture))
            ];
        }
        if (FilterCreatedAtBefore.HasValue)
        {
            dto.FilterCreatedAtBefore =
                new DateTimeOffset(FilterCreatedAtBefore.Value, TimeSpan.Zero)
                    .ToUnixTimeSeconds()
                    .ToString("D", CultureInfo.InvariantCulture);
        }
        if (FilterCreatedAtAfter.HasValue)
        {
            dto.FilterCreatedAtAfter =
                new DateTimeOffset(FilterCreatedAtAfter.Value, TimeSpan.Zero)
                    .ToUnixTimeSeconds()
                    .ToString("D", CultureInfo.InvariantCulture);
        }

        if (RequestingGuildId.HasValue)
        {
            dto.RequestingGuildId = RequestingGuildId.Value
                .ToString("D", CultureInfo.InvariantCulture);
        }

        return dto;
    }

    public BanSyncRecordRepository.SearchQueryOptions ToRepositoryOptions()
    {
        return new BanSyncRecordRepository.SearchQueryOptions()
        {
            FilterUserIds = FilterUserIds,
            FilterCreatedByUserIds = FilterCreatedByUserIds,
            FilterGuildIds = FilterGuildIds,
            FilterCreatedAtBefore = FilterCreatedAtBefore,
            FilterCreatedAtAfter = FilterCreatedAtAfter,
            SortBy = SortBy switch
            {
                BanSyncSearchApiQuerySortBy.CreatedAt => SearchQuerySortBy.CreatedAt,
                BanSyncSearchApiQuerySortBy.GuildId => SearchQuerySortBy.GuildId,
                BanSyncSearchApiQuerySortBy.UserId => SearchQuerySortBy.UserId,
                BanSyncSearchApiQuerySortBy.Username => SearchQuerySortBy.Username,
                _ => SearchQuerySortBy.CreatedAt
            },
            SortDirection = SortDirection switch
            {
                BanSyncSearchApiQuerySortDirection.Ascending => SearchQuerySortDirection.Ascending,
                BanSyncSearchApiQuerySortDirection.Descending => SearchQuerySortDirection.Descending,
                _ => SearchQuerySortDirection.Descending
            },
            GhostState = GhostState,
            PageIndex = Pagination.Page - 1,
            PageSize = Pagination.Limit
        };
    }
}

public record MaybeRecord<TValue>(TValue Value, bool HasValue);