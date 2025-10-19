using Discord;
using XeniaDiscord.Data.Models.Confession;

namespace XeniaDiscord.Common.Interfaces;

public interface IConfessionService
{
    public Task<EmbedBuilder> CreateAsync(IGuild guild, IUser user, string content);
    public Task<EmbedBuilder> SetOutputChannelAsync(IGuild guild, ITextChannel channel, IUser? createdByUser);
    public Task<EmbedBuilder> SetOutputChannelAsync(IGuild guild, ITextChannel channel);
    public Task<IUserMessage> SendModalMessage(GuildConfessionConfigModel configModel, IGuild guild, ITextChannel channel);
    public Task<GuildConfessionConfigModel> GetOrCreateGuildConfig(IGuild guild, IUser? createdByUser);
    public Task<GuildConfessionConfigModel> GetOrCreateGuildConfig(IGuild guild);
}
