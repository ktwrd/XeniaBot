using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.ComponentModel;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;

namespace XeniaDiscord.Common;

public static class StartupExtensions
{
    public static void WithDatabaseServices(this IServiceCollection services) => services.WithDatabaseServices(new());
    public static void WithDatabaseServices(this IServiceCollection services, DatabaseServicesOptions options)
    {
        // Add services to the container.
        services.AddDbContextPool<XeniaDbContext>(ConfigureDbContextOptionsBuilder);
        services.AddPooledDbContextFactory<XeniaDbContext>(ConfigureDbContextOptionsBuilder);
        // TODO for asp.net
        // if (options.DatabaseDeveloperPageExceptionFilter)
        // {
        //     services.AddDatabaseDeveloperPageExceptionFilter();
        // }
    }

    private static void ConfigureDbContextOptionsBuilder(
        DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = CoreContext.Instance!.Config.Data.Postgres.ToConnectionString();
        Log.Debug(connectionString);
        optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.EnableSensitiveDataLogging();
    }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public class DatabaseServicesOptions
    {
        /// <summary>
        /// Enabled in development mode.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableSensitiveDataLogging { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(false)]
        public bool DatabaseDeveloperPageExceptionFilter { get; set; } = false;
    }
}
