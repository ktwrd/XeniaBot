using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using XeniaDiscord.Data;

namespace XeniaDiscord.Interactions;

public static class ModuleHelper
{
    /// <summary>
    /// Callback for <see cref="PerformTransaction(IServiceProvider, PerformTransactionCallback)"/>
    /// </summary>
    /// <param name="db"></param>
    /// <returns>
    /// <see langword="true"/> if the transaction should commit.
    /// Otherwise, it will rollback the transaction.
    /// </returns>
    public delegate Task<bool> PerformTransactionCallback(XeniaDbContext db);
    
    public static Task<TimeSpan> PerformTransaction(IServiceProvider services, PerformTransactionCallback callback)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        return PerformTransaction(dbContextFactory, callback);
    }
    public static async Task<TimeSpan> PerformTransaction(IDbContextFactory<XeniaDbContext> dbContextFactory, PerformTransactionCallback callback)
    {
        var sw = new Stopwatch();
        sw.Start();
        await using var db = await dbContextFactory.CreateDbContextAsync();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            if (await callback(db))
            {
                await db.SaveChangesAsync();
                await trans.CommitAsync();
            }
            else
            {
                await trans.RollbackAsync();
            }
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
        finally
        {
            sw.Stop();
        }
        return sw.Elapsed;
    }
}
