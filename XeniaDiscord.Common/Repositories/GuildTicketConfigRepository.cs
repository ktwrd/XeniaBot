using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Ticket;

namespace XeniaDiscord.Common.Repositories;

public class GuildTicketConfigRepository : IGuildTicketConfigRepository
{
    private readonly ApplicationDbContext _db;
    public GuildTicketConfigRepository(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    public async Task<GuildTicketConfigModel?> GetAsync(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await _db.GuildTicketConfigs.AsNoTracking().FirstOrDefaultAsync(e => e.Id == guildIdStr);
    }
}
