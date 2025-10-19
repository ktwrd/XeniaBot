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
}
