using XeniaDiscord.Data.Models.Ticket;

namespace XeniaDiscord.Common.Interfaces;

public interface ITicketService
{
    public Task<GuildTicketModel> CreateTicket(ulong guildId);
    public Task UserAccessGrant(ulong channelId, ulong userId);
    public Task UserAccessRevoke(ulong channelId, ulong userId);
    public Task<GuildTicketTranscriptModel> CloseTicket(ulong channelId, GuildTicketStatus status, ulong closingUserId);
}
