using System;
using System.Globalization;
using System.Linq;
using CSharpFunctionalExtensions;
using XeniaDiscord.Data.Repositories;

using SearchQuerySortBy = XeniaDiscord.Data.Repositories.BanSyncRecordRepository.SearchQuerySortBy;
using SearchQuerySortDirection = XeniaDiscord.Data.Repositories.BanSyncRecordRepository.SearchQuerySortDirection;
// ReSharper disable RedundantSwitchExpressionArms

namespace XeniaBot.WebPanel.Models.BanSyncSearch;

public class BanSyncSearchApiQuery
{
    /// <summary>
    /// 2015-01-01 00:00:00 UTC
    /// </summary>
    public const int FilterEpoch = 1420070400;

    public Maybe<ulong[]> FilterUserIds { get; init; } = Maybe.None;
    public Maybe<ulong[]> FilterCreatedByUserIds { get; init; } = Maybe.None;
    public Maybe<ulong[]> FilterGuildIds { get; init; } = Maybe.None;
    public Maybe<DateTime> FilterCreatedAtBefore { get; init; } = Maybe.None;
    public Maybe<DateTime> FilterCreatedAtAfter { get; init; } = Maybe.None;
    public required BanSyncSearchApiQuerySortBy SortBy { get; init; }
    public required BanSyncSearchApiQuerySortDirection SortDirection { get; init; }
    /// <summary>
    /// This can only be <c>0</c> or None if the requestor is a developer
    /// </summary>
    public Maybe<ulong> RequestingGuildId { get; init; }
    public required bool IncludeRequestingGuildRecordsInResult { get; init; }
    public required bool EnforceGuildVisibility { get; set; }
    public bool? GhostState { get; init; }
    public required BanSyncSearchApiQueryPagination Pagination { get; init; }

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