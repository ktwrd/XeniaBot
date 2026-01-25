using Discord;
using XeniaDiscord.Data.Models.Warn;

namespace XeniaDiscord.Common;

public interface IWarnRepository
{
    public Task<GuildWarnConfigModel> GetOrCreateConfig(IGuild guild, IUser? createdByUser);
    public Task<GuildWarnConfigModel> UpdateLogChannel(ITextChannel channel, IGuild guild, IUser? updatedByUser);
    public Task<bool> EnableLogging(IGuild guild, IUser? updatedByUser);
    public Task<bool> DisableLogging(IGuild guild, IUser? updatedByUser);
}
