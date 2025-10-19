using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Ticket;

namespace XeniaDiscord.Common.Repositories;

public class GuildTicketRepository
{
    private readonly ApplicationDbContext _db;
    public GuildTicketRepository(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    public async Task<GuildTicketModel?> GetForChannelAsync(IGuildChannel channel)
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
}
