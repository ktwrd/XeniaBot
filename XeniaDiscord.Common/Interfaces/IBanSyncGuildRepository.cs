using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Common.Interfaces;

public interface IBanSyncGuildRepository
{
    public Task<BanSyncGuildModel?> GetAsync(ulong guildId);
    public Task<BanSyncGuildModel> UpdateAsync(BanSyncGuildModel model, Discord.IUser? updatedBy = null);
}
