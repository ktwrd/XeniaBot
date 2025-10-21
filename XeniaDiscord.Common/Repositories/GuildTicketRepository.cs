using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Ticket;

namespace XeniaDiscord.Common.Repositories;

public class GuildTicketRepository : IGuildTicketRepository
{
    private readonly ApplicationDbContext _db;
    public GuildTicketRepository(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    public async Task<GuildTicketModel?> GetForChannelAsync(Discord.IGuildChannel channel)
    {
        var guildId = channel.GuildId.ToString();
        var channelId = channel.Id.ToString();
        return await _db.GuildTickets.AsNoTracking()
            .FirstOrDefaultAsync(e => e.GuildId == guildId && e.ChannelId == channelId);
    }
    public async Task<GuildTicketModel?> GetForChannelAsync(ulong channel)
    {
        var channelId = channel.ToString();
        return await _db.GuildTickets.AsNoTracking()
            .FirstOrDefaultAsync(e => e.ChannelId == channelId);
    }

    public async Task<GuildTicketModel> UpdateAsync(GuildTicketModel model)
    {
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            for (int i = 0; i < model.Users.Count; i++) model.Users[i].TicketId = model.Id;
            model.Users = model.Users.DistinctBy(e => e.UserId).ToList();

            var existing = await ctx.GuildTickets.Include(e => e.Users).AsNoTracking().Where(e => e.Id == model.Id).FirstOrDefaultAsync();
            var existingUsers = await ctx.GuildTicketUsers.Where(e => e.TicketId == model.Id).ToListAsync();
            
            var affected = await ctx.GuildTickets.Where(e => e.Id == model.Id).ExecuteUpdateAsync(e => e
                .SetProperty(p => p.GuildId, model.GuildId)
                .SetProperty(p => p.ChannelId, model.ChannelId)
                .SetProperty(p => p.ClosedAt, model.ClosedAt)
                .SetProperty(p => p.Status, model.Status)
                .SetProperty(p => p.ClosedByUserId, model.ClosedByUserId));
            if (affected == 0)
            {
                for (int i = 0; i < model.Users.Count; i++) model.Users[i].TicketId = model.Id;
                await ctx.GuildTickets.AddAsync(model);
                await ctx.GuildTicketUsers.AddRangeAsync(model.Users);
            }
            else if (existing != null)
            {
                var targetRemove = existingUsers.Where(e => !model.Users.Any(x => x.UserId == e.UserId)).ToArray();
                ctx.GuildTicketUsers.RemoveRange(targetRemove);
                    await ctx.GuildTicketUsers.AddRangeAsync(model.Users.Where(e => !existingUsers.Any(x => x.UserId == e.UserId)));
            }

            await ctx.SaveChangesAsync();
            await trans.CommitAsync();

            return await ctx.GuildTickets
                .Include(e => e.Users).AsNoTracking()
                .SingleAsync(e => e.Id == model.Id); ;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
