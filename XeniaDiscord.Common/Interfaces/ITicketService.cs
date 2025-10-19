using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaDiscord.Common.Interfaces;

public interface ITicketService
{
    public Task<GuildTicketModel> CreateTicket(ulong guildId);
    public Task UserAccessGrant(ulong channelId, ulong userId);
    public Task UserAccessRevoke(ulong channelId, ulong userId);
    public async Task<TicketTranscriptModel> CloseTicket(ulong channelId, TicketStatus status, ulong closingUserId);
}
