using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using XeniaDiscord.Data;

namespace XeniaDiscord.Interactions;

public static class ModuleHelper
{

    /// <summary>
    /// Callback for <see cref="PerformTransaction(Func{XeniaDbContext, Task{bool}})"/>
    /// </summary>
    /// <param name="db"></param>
    /// <returns>
    /// <see langword="true"/> if the transaction should commit.
    /// Otherwise, it will rollback the transaction.
    /// </returns>
    public delegate Task<bool> PerformTransactionCallback(XeniaDbContext db);
    public static async Task<TimeSpan> PerformTransaction(IServiceProvider services, PerformTransactionCallback callback)
    {
        var sw = new Stopwatch();
        sw.Start();
        await using var db = services.GetRequiredService<XeniaDbContext>().CreateSession();
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
