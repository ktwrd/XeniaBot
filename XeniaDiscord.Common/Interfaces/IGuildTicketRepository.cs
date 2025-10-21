using XeniaDiscord.Data.Models.Ticket;

namespace XeniaDiscord.Common.Interfaces;

public interface IGuildTicketRepository
{
    public Task<GuildTicketModel?> GetForChannelAsync(Discord.IGuildChannel channel);
    public Task<GuildTicketModel?> GetForChannelAsync(ulong channel);
    public Task<GuildTicketModel> UpdateAsync(GuildTicketModel model);
}
