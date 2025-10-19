using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Common.Repositories;

public class BanSyncGuildRepository
{
    private readonly ApplicationDbContext _db;
    public BanSyncGuildRepository(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    public async Task<BanSyncGuildModel?> GetAsync(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await _db.BanSyncGuilds.AsNoTracking().FirstOrDefaultAsync(e => e.Id == guildIdStr);
    }

    public async Task<BanSyncGuildModel> UpdateAsync(BanSyncGuildModel model, Discord.IUser? updatedBy = null)
    {
        var updatedByUserId = updatedBy?.Id.ToString();

        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var mdl = model.Clone();
            mdl.UpdatedAt = now;
            mdl.UpdatedByUserId = updatedByUserId;

            var snapshot = model.ToSnapshot();
            snapshot.Timestamp = now;

            await ctx.BanSyncGuildSnapshots.AddAsync(snapshot);
            var affected = await ctx.BanSyncGuilds.Where(e => e.Id == mdl.Id).ExecuteUpdateAsync(e => e
                .SetProperty(p => p.GuildName, mdl.GuildName)
                .SetProperty(p => p.LogChannelId, mdl.LogChannelId)
                .SetProperty(p => p.Enabled, mdl.Enabled)
                .SetProperty(p => p.State, mdl.State)
                .SetProperty(p => p.InternalNote, mdl.InternalNote)
                .SetProperty(p => p.CreatedAt, mdl.CreatedAt)
                .SetProperty(p => p.UpdatedAt, mdl.UpdatedAt)
                .SetProperty(p => p.UpdatedByUserId, mdl.UpdatedByUserId));
            if (affected == 0)
            {
                await ctx.BanSyncGuilds.AddAsync(mdl);
            }

            await ctx.SaveChangesAsync();
            await trans.CommitAsync();

            return mdl;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
