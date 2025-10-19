using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Common.Repositories;

public class BanSyncRepository
{
    private readonly ApplicationDbContext _db;
    public BanSyncRepository(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    public async Task<bool> AnyAsync(Discord.IGuild guild, Discord.IBan ban)
    {
        var userId = ban.User.Id.ToString();
        var guildId = guild.Id.ToString();
        return await _db.BanSyncRecords.AnyAsync(e => e.GuildId == guildId && e.UserId == userId);
    }
    public async Task<bool> AnyAsync(Discord.IGuild guild, Discord.IUser user)
    {
        var userId = user.Id.ToString();
        var guildId = guild.Id.ToString();
        return await _db.BanSyncRecords.AnyAsync(e => e.GuildId == guildId && e.UserId == userId);
    }
    public async Task<ICollection<BanSyncRecordModel>> GetAllForUser(Discord.IUser user, int? limit = null)
    {
        var userId = user.Id.ToString();
        var q = _db.BanSyncRecords.AsNoTracking()
            .Where(e => e.UserId == userId);
        if (limit.HasValue && limit.Value > 0)
            return await q.OrderByDescending(e => e.CreatedAt).Take(limit.Value).ToListAsync();

        return await q.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }
    public async Task<long> GetCountForUser(Discord.IUser user)
    {
        var userId = user.Id.ToString();
        return await _db.BanSyncRecords.Where(e => e.UserId == userId).LongCountAsync();
    }

    public async Task<BanSyncRecordModel> CreateAsync(Discord.IGuild guild, Discord.IBan ban)
    {
        var model = new BanSyncRecordModel()
        {
            UserId = ban.User.Id.ToString(),
            GuildId = guild.Id.ToString(),
            GuildName = guild.Name,
            Reason = ban.Reason,

            Username = ban.User.Username,
            DisplayName = ban.User.GlobalName,
        };
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await ctx.BanSyncRecords.AddAsync(model);
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
        return model;
    }
}
