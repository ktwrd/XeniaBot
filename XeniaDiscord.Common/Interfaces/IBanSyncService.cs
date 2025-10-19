using Discord;
using Discord.WebSocket;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Common.Interfaces;

public interface IBanSyncService
{
    public Task UpdateLogChannel(IGuild guild, ITextChannel channel, IUser updatedBy);
    public Task RefreshBans(SocketGuild guild);
    public Task NotifyBan(BanSyncRecordModel info);
    public Task<EmbedBuilder> GenerateEmbed(ICollection<BanSyncRecordModel> data, long totalCount);
    public Task<BanSyncGuildKind> GetGuildKind(ulong guildId);
    public Task<BanSyncGuildModel?> SetGuildState(ulong guildId,
        BanSyncGuildState state,
        string reason = "",
        bool doRefreshBans = true,
        IUser? updatedBy = null);
    public Task<BanSyncGuildModel> RequestGuildEnable(ulong guildId, IUser? requestedBy = null);
}
