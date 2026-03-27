using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;

namespace XeniaDiscord.Data.Services;

public class DatabaseMigrationService : BaseService
{
    private readonly ErrorReportService _err;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public DatabaseMigrationService(IServiceProvider services) : base(services)
    {
        _err = services.GetRequiredService<ErrorReportService>();
    }

    /// <summary>
    /// Called when all services have been added to the collection.
    /// </summary>
    /// <returns></returns>
    public override async Task InitializeAsync()
    {
        using var scope = _services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<XeniaDbContext>();

        var migrationsEnumerable = await db.Database.GetPendingMigrationsAsync();
        var migrationsArray = migrationsEnumerable.ToArray();
        if (migrationsArray.Length < 1)
        {
            _log.Info("No pending migrations.");
            return;
        }

        var migrationCount = migrationsArray.Length.ToString("n0");
        var joined = string.Join("\n", migrationsArray.Select(e => "- " + e));
        _log.Info($"Applying {migrationCount} pending migration(s)\n{joined}");
        try
        {
            await db.Database.MigrateAsync();
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            var msg = $"Failed to apply {migrationCount} migration(s)";
            _log.Error(ex, msg);
            await _err.ReportException(ex, msg,
                new Dictionary<string, string>()
                {
                    {"migrations.txt", string.Join("\n", migrationsArray) }
                });
        }
    }

    /// <summary>
    /// Not implemented
    /// </summary>
    public override Task OnReady()
    {
        // not implemented
        return Task.CompletedTask;
    }

    /// <summary>
    /// Not implemented
    /// </summary>
    public virtual Task OnReadyDelay()
    {
        // not implemented
        return Task.CompletedTask;
    }
}
