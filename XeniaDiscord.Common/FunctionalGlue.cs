using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Reflection;
using XeniaBot.Shared;
using XeniaDiscord.Data;

namespace XeniaDiscord.Common;

public static class FunctionalGlue
{
    public static void ApplyDatabaseMigrations(IServiceProvider services)
    {
        var log = LogManager.GetLogger(nameof(FunctionalGlue) + "." + nameof(ApplyDatabaseMigrations));

        var db = services.GetRequiredService<ApplicationDbContext>();
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Count != 0)
        {
            var migrationNames = string.Join(Environment.NewLine, pendingMigrations.Select(e => "- " + e));
            log.Info("Performing database migrations:" + Environment.NewLine + migrationNames);
            try
            {
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                log.Fatal(ex, "Failed to apply database migrations!!!!!");
#if DEBUG
                System.Diagnostics.Debugger.BreakForUserUnhandledException(ex);
#endif
                Environment.Exit(1);
            }
        }
    }

    public static IReadOnlyList<Type> FindAndRegisterServices(IServiceCollection serviceCollection, params Assembly[] assemblies)
    {
        var result = new List<Type>();
        foreach (var asm in assemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                if (!typeof(IXeniaService).IsAssignableFrom(type)) continue;
                result.Add(type);
                serviceCollection.AddScoped(type);
            }
        }
        return result.AsReadOnly();
    }

    public static IEnumerable<ServiceDescriptor> FindServicesThatExtend<T>(IServiceCollection services)
    {
        return services.Where(x => typeof(T).IsAssignableFrom(x.ServiceType));
    }

    public static void NotifyDiscordReady(
        IReadOnlyList<Type> registeredServices,
        IServiceProvider services,
        bool rethrow = false)
    {
        var logger = LogManager.GetLogger(nameof(FunctionalGlue) + "." + nameof(NotifyDiscordReady));
        try
        {
            foreach (var type in registeredServices)
            {
                var s = services.GetRequiredService(type);
                if (s is IXeniaService svc)
                {
                    svc.OnDiscordReady().Wait();
                }
            }
            logger.Trace($"Successfully called {nameof(IXeniaService.OnDiscordReady)} on all valid services.");
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Failed to notify services that DiscordShardedClient is ready");
            if (rethrow) throw;
        }
    }
}
