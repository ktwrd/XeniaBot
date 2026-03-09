using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Npgsql;
using XeniaBot.Shared;

namespace XeniaDiscord.Data;

public static class Extensions
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
            ApplicationName = "XeniaDiscord"
        };
        return b.ConnectionString;
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

    public static TService GetRequiredScopedService<TService>(
        this IServiceProvider services,
        IServiceScopeCallbackDelegate initializeScope,
        out IServiceScope? scope)
    {
        scope = null;
        var result = services.GetService<TService>();
        if (ReferenceEquals(result, null))
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
}
public delegate void IServiceScopeCallbackDelegate(IServiceScope scope);
