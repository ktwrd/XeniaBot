using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaDiscord.Data.Models.GuildApproval;

namespace XeniaDiscord.Data.Repositories;

public class GuildApprovalRepository
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public async Task InsertOrUpdate(XeniaDbContext db, GuildApprovalModel model)
    {
        if (await db.GuildApprovals.AnyAsync(e => e.GuildId == model.GuildId))
        {
            await db.GuildApprovals.Where(e => e.GuildId == model.GuildId)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.ApprovedRoleId, model.ApprovedRoleId)
                .SetProperty(p => p.ApproverRoleId, model.ApproverRoleId)
                .SetProperty(p => p.LogChannelId, model.LogChannelId)
                .SetProperty(p => p.Enabled, model.Enabled)
                .SetProperty(p => p.EnableGreeter, model.EnableGreeter)
                .SetProperty(p => p.GreeterChannelId, model.GreeterChannelId)
                .SetProperty(p => p.GreeterMessageTemplate, model.GreeterMessageTemplate)
                .SetProperty(p => p.GreeterAsEmbed, model.GreeterAsEmbed)
                .SetProperty(p => p.GreeterMentionUser, model.GreeterMentionUser));
            _log.Debug($"Updated record (GuildId={model.GuildId})");
        }
        else
        {
            model.GetGuildId();
            await db.GuildApprovals.AddAsync(model);
            _log.Debug($"Inserted record (GuildId={model.GuildId})");
        }
    }
}