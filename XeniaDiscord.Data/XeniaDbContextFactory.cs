using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;

namespace XeniaDiscord.Data;

public class XeniaDbContextFactory : IDesignTimeDbContextFactory<XeniaDbContext>
{
    XeniaDbContext IDesignTimeDbContextFactory<XeniaDbContext>.CreateDbContext(string[] args)
    {
        var pv = Environment.GetEnvironmentVariable("CONFIG_READONLY");
        var builder = new DbContextOptionsBuilder<XeniaDbContext>();
        Environment.SetEnvironmentVariable("CONFIG_READONLY", "true");
        var cfgSvc = new ConfigService(new ProgramDetails()
        {
            Platform = XeniaPlatform.Bot
        });
        var connectionString = cfgSvc.Data.Postgres.ToConnectionString();
        if (!connectionString.EndsWith(';'))
            connectionString += ";";
        connectionString += "Include Error Detail=true";
        builder.UseNpgsql(connectionString);
        Environment.SetEnvironmentVariable("CONFIG_READONLY", pv);

        return new XeniaDbContext(builder.Options);
    }
}
