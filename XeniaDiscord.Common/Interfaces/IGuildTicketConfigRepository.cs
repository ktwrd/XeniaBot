using XeniaDiscord.Data.Models.Ticket;

namespace XeniaDiscord.Common.Interfaces;

public interface IGuildTicketConfigRepository
{
    public Task<GuildTicketConfigModel?> GetAsync(ulong guildId);
}
