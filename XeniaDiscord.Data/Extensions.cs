using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Models.PartialSnapshot;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Data;

public static class DataExtensions
{
    public static string ToConnectionString(this PostgresConfigItem config)
    {
        var b = new NpgsqlConnectionStringBuilder()
        {
            Host = config.Host,
            Port = config.Port,
            Username = config.Username,
            Password = config.Password,
            Database = config.DatabaseName,
            ApplicationName = "XeniaDiscord",
            IncludeErrorDetail = true,
        };
        return b.ConnectionString;
    }

    public static string FormatUsername(this UserSnapshotModel user)
    {
        if (string.IsNullOrEmpty(user.Discriminator?.Trim()?.Trim('0'))) return user.Username;
        return $"{user.Username}#{user.Discriminator}";
    }

    public static string FormatUsername(this UserPartialSnapshotModel user)
    {
        if (string.IsNullOrEmpty(user.Discriminator?.Trim()?.Trim('0'))) return user.Username;
        return $"{user.Username}#{user.Discriminator}";
    }

    public static ulong ParseRequiredULong(this string? value, string propertyName, bool allowZero = true)
    {
        if (string.IsNullOrEmpty(value?.Trim())) throw new InvalidOperationException($"Property {propertyName} is null or empty");
        if (ulong.TryParse(value.Trim(), out var result) &&
            (allowZero || result > 0)) return result;
        throw new InvalidOperationException($"Failed to parse property {propertyName} with value: {value}");
    }
    public static ulong? ParseULong(this string? value, bool allowZero = true)
    {
        if (ulong.TryParse(value?.Trim(), out var result) &&
            (allowZero || result > 0)) return result;
        return null;
    }

    public static uint? ParseUInt(this string? value, bool allowZero = true)
    {
        if (uint.TryParse(value?.Trim(), out var result) &&
            (allowZero || result > 0)) return result;
        return null;
    }

    public static TService GetRequiredScopedService<TService>(
        this IServiceProvider services,
        IServiceScopeCallbackDelegate initializeScope,
        out IServiceScope? scope)
    {
        scope = null;
        TService result;
        try
        {
            result = services.GetRequiredService<TService>();
        }
        catch
        {
            scope = services.CreateScope();
            initializeScope(scope!);
            result = scope.ServiceProvider.GetRequiredService<TService>();
        }
        return result;
    }
    public static TService GetRequiredScopedService<TService>(
        this IServiceProvider services,
        out IServiceScope? scope)
    {
        scope = null;
        return services.GetRequiredScopedService<TService>(IServiceScopeCallbackDelegateDefault, out scope);
    }
    private static readonly IServiceScopeCallbackDelegate IServiceScopeCallbackDelegateDefault = scope => { };


    public static IQueryable<TModel> ApplyPagination<TModel>(this IQueryable<TModel> query, PaginationOptions options)
    {
        return query
            .Skip(options.Skip)
            .Take(options.PageSize);
    }
    public static ModelBuilder AuditLogCacheEntity<TEntity>(
        this ModelBuilder modelBuilder,
        Action<EntityTypeBuilder<TEntity>> buildAction)
        where TEntity : BaseAuditLogEntryCacheModel
    {
        return modelBuilder.Entity<TEntity>(builder =>
        {
            builder.HasKey(e => e.Id);
            builder.HasIndex(e => new
            {
                e.Id,
                e.GuildId,
                e.CreatedAt,
                e.Action
            });
            builder.HasIndex(e => new
            {
                e.Id,
                e.GuildId,
                e.CreatedAt,
                e.Action,
                e.PerformedByUserId
            });

            buildAction(builder);
        });
    }
}
public delegate void IServiceScopeCallbackDelegate(IServiceScope scope);

public class PaginationOptions
{
    /// <summary>
    /// Min value: 1
    /// </summary>
    public int Page
    {
        get;
        init => field = Math.Max(1, value);
    } = 1;

    /// <summary>
    /// Min value: 1
    /// </summary>
    public int PageSize
    {
        get;
        init => field = Math.Max(1, value);
    } = 15;

    public int Skip => Page > 1 ? (Page - 1) * PageSize : 0;
    public int Take => PageSize;
}