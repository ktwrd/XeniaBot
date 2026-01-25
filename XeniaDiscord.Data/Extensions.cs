using Npgsql;
using XeniaBot.Shared.Config;

namespace XeniaDiscord.Data;

public static class Extensions
{
    public static string ToConnectionString(this PostgreSQLConfigElement element)
    {
        var b = new NpgsqlConnectionStringBuilder();
        b.Host = element.Host;
        b.Port = element.Port;
        b.Username = element.Username;
        b.Password = element.Password;
        b.Database = element.Name;
        b.ApplicationName = "XeniaDiscord";
        return b.ToString();
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
}
