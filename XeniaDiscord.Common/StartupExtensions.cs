using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddDbContextPool<XeniaDbContext>(
            o =>
            {
                var connectionString = CoreContext.Instance!.Config.Data.Postgres.ToConnectionString();
                o.UseNpgsql(connectionString);

                if (options.EnableSensitiveDataLogging)
                {
                    o.EnableSensitiveDataLogging();
                }
            });
        // TODO for asp.net
        // if (options.DatabaseDeveloperPageExceptionFilter)
        // {
        //     services.AddDatabaseDeveloperPageExceptionFilter();
        // }
    }

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
