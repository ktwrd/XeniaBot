using Discord;
using Microsoft.Extensions.DependencyInjection;

namespace XeniaDiscord.Common.Services;

public class WarnAdminService : IWarnAdminService
{
    private readonly IDiscordClient _discord;
    private readonly IWarnRepository _warnRepo;
    public WarnAdminService(IServiceProvider services)
    {
        _discord = services.GetRequiredService<IDiscordClient>();
        _warnRepo = services.GetRequiredService<IWarnRepository>();
    }

    public async Task<WarnAdminSetLogChannelResponseKind> SetLogChannel(
        IGuild guild,
        IUser createdByUser,
        ITextChannel targetChannel)
    {
        IGuildUser? guildUser = null;
        IGuildUser? selfGuildUser = null;
        try
        {
            guildUser = await guild.GetUserAsync(createdByUser.Id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get User {createdByUser.Username} ({createdByUser.Id}) in Guild \"{guild.Name}\" ({guild.Id})", ex);
        }
        try
        {
            selfGuildUser = await guild.GetUserAsync(_discord.CurrentUser.Id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get own user {_discord.CurrentUser.Username} ({_discord.CurrentUser.Id}) in Guild \"{guild.Name}\" ({guild.Id})", ex);
        }
        if (guildUser == null)
            return WarnAdminSetLogChannelResponseKind.MissingPermissions;
        if (selfGuildUser == null)
            throw new InvalidOperationException($"Own user ({_discord.CurrentUser.Id}) isn't in guild provided (\"{guild.Name}\", {guild.Id})");

        var ownPermissions = selfGuildUser.GetPermissions(targetChannel);
        if (!ownPermissions.SendMessages)
        {
            return WarnAdminSetLogChannelResponseKind.CannotAccessChannel;
        }

        if (!guildUser.GuildPermissions.ManageChannels)
        {
            return WarnAdminSetLogChannelResponseKind.MissingPermissions;
        }

        var config = await _warnRepo.UpdateLogChannel(targetChannel, guild, createdByUser);
        if (config.CreatedAt > DateTime.UtcNow - TimeSpan.FromMinutes(1))
        {
            await _warnRepo.EnableLogging(guild, createdByUser);
        }

        return WarnAdminSetLogChannelResponseKind.Success;
    }
}
