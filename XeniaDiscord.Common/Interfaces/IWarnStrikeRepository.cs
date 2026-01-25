using Discord;
using XeniaDiscord.Data.Models.Warn;

namespace XeniaDiscord.Common;

public interface IWarnStrikeRepository
{
    public IQueryable<GuildWarnModel> GetActiveStrikesForUserQuery(GuildWarnStrikeConfigModel warnStrikeConfig, string userId);

    public Task<List<GuildWarnModel>> GetActiveStrikesForUser(ulong guildId, ulong userId);
    public Task<List<GuildWarnModel>> GetActiveStrikesForUser(IGuild guild, IUser user);
    public Task<List<GuildWarnModel>> GetActiveStrikesForUser(IGuildUser user);

    public Task<GuildWarnStrikeConfigModel?> GetWarnStrikeConfig(ulong guildId);
    public Task<GuildWarnStrikeConfigModel?> GetWarnStrikeConfig(IGuild guild);
    public Task<GuildWarnStrikeConfigModel?> GetWarnStrikeConfig(GuildWarnModel warnRecord);
    public Task<GuildWarnStrikeConfigModel?> GetWarnStrikeConfig(GuildWarnConfigModel guildConfig);
}
