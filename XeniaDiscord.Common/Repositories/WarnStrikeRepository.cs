using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Warn;

namespace XeniaDiscord.Common.Repositories;

public class WarnStrikeRepository : IWarnStrikeRepository
{
    private readonly ApplicationDbContext _db;
    public WarnStrikeRepository(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    public IQueryable<GuildWarnModel> GetActiveStrikesForUserQuery(GuildWarnStrikeConfigModel warnStrikeConfig, string userId)
    {
        var guildId = warnStrikeConfig.Id;
        var beforeDate = DateTime.UtcNow - TimeSpan.FromMinutes(warnStrikeConfig.StrikeAliveTime);
        return _db.GuildWarns
            .Where(e => e.GuildId == guildId && e.TargetUserId == userId)
            .Where(e => e.CreatedAt > beforeDate);
    }

    public async Task<List<GuildWarnModel>> GetActiveStrikesForUser(ulong guildId, ulong userId)
    {
        var guildIdStr = guildId.ToString();
        var userIdStr = userId.ToString();
        var guildConfig = await _db.GuildWarnStrikeConfigs
            .Where(e => e.Id == guildIdStr && e.Enabled)
            .FirstOrDefaultAsync();
        if (guildConfig == null) return [];
        return await GetActiveStrikesForUserQuery(guildConfig, userIdStr).ToListAsync();
    }
    public Task<List<GuildWarnModel>> GetActiveStrikesForUser(IGuild guild, IUser user)
        => GetActiveStrikesForUser(guild.Id, user.Id);
    public Task<List<GuildWarnModel>> GetActiveStrikesForUser(IGuildUser user)
        => GetActiveStrikesForUser(user.GuildId, user.Id);

    private async Task<GuildWarnStrikeConfigModel?> GetWarnStrikeConfig(string guildIdStr)
    {
        var record = await _db.GuildWarnStrikeConfigs
            .FirstOrDefaultAsync(e => e.Id == guildIdStr);
        return record;
    }

    public Task<GuildWarnStrikeConfigModel?> GetWarnStrikeConfig(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return GetWarnStrikeConfig(guildIdStr);
    }
    public Task<GuildWarnStrikeConfigModel?> GetWarnStrikeConfig(IGuild guild)
        => GetWarnStrikeConfig(guild.Id);
    public Task<GuildWarnStrikeConfigModel?> GetWarnStrikeConfig(GuildWarnModel warnRecord)
        => GetWarnStrikeConfig(warnRecord.GuildId);
    public Task<GuildWarnStrikeConfigModel?> GetWarnStrikeConfig(GuildWarnConfigModel guildConfig)
        => GetWarnStrikeConfig(guildConfig.Id);
}
