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

    private bool hasInitialized = false;

    /// <inheritdoc/>
    /// <remarks>
    /// Creates a new thread to actually do the work.
    /// This method will not exit until <see cref="InitializeThread"/> has finished running.
    /// </remarks>
    public override async Task InitializeAsync()
    {
        if (hasInitialized) return;

        new Thread(() =>
        {
            try
            {
                InitializeThread().Wait();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to call {nameof(InitializeThread)}");
            }
            finally
            {
                hasInitialized = true;
            }
        })
        {
            Name = $"Xenia.{nameof(DatabaseMigrationService)}.{nameof(InitializeAsync)}"
        }.Start();

        while (!hasInitialized) await Task.Delay(500);
    }

    private async Task InitializeThread()
    {
        using var scope = Services.CreateScope();
        await using var db = scope.ServiceProvider.GetRequiredService<XeniaDbContext>();

        var migrationsEnumerable = await db.Database.GetPendingMigrationsAsync();
        var migrationsArray = migrationsEnumerable.ToArray();
        if (migrationsArray.Length < 1)
        {
            _log.Info("No pending migrations");
            hasInitialized = true;
            return;
        }

        var migrationCount = migrationsArray.Length.ToString("n0");
        var joined = string.Join("\n", migrationsArray.Select(e => "- " + e));
        _log.Info($"Applying {migrationCount} pending migration(s)\n{joined}");
        try
        {
            await db.Database.MigrateAsync();
            await db.SaveChangesAsync();
            hasInitialized = true;
        }
        catch (Exception ex)
        {
            hasInitialized = true;
            var msg = $"Failed to apply {migrationCount} migration(s)";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .AddSerializedAttachment("migrations.txt", string.Join("\n", migrationsArray)));
        }
    }
}
