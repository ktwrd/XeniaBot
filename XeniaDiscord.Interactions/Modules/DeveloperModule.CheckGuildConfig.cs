using Discord;
using Discord.Interactions;
using XeniaDiscord.Data;

namespace XeniaDiscord.Interactions.Modules;

partial class DeveloperModule
{
    [SlashCommand("chk-guild-cfg", "Validate Guild Config")]
    public async Task ValidateGuildConfig(
        string guildId,
        ValidateGuildConfigAction action)
    {
        await DeferAsync();
        var guildIdReal = guildId.ParseRequiredULong(nameof(guildId), true);
        switch (action)
        {
            case ValidateGuildConfigAction.BanSync:
                await ValidateBanSyncConfigAction(guildIdReal);
                break;
        }
    }

    public enum ValidateGuildConfigAction
    {
        BanSync
    }

    private async Task ValidateBanSyncConfigAction(
        ulong guildId)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Validate BanSync Guild Config")
            .WithFooter(new EmbedFooterBuilder()
                .WithText($"Guild: {guildId}"))
            .WithCurrentTimestamp()
            .WithColor(Color.Red);
        var guildConfig = await _bansyncGuildRepo.GetAsync(guildId);
        if (guildConfig == null)
        {
            embed.WithDescription("BanSync has not been configured for this guild.");
            await FollowupAsync(embeds: [embed.Build()]);
            return;
        }
        var files = new List<FileAttachment>()
        {
            CreateAttachment("bansyncGuild.json", guildConfig)
        };


        var logchannelId = guildConfig.GetLogChannelId();
        if (!logchannelId.HasValue)
        {
            embed.WithDescription("Log Channel Id not configured.");
            await FollowupWithFilesAsync(files, embeds: [embed.Build()]);
            return;
        }

        var validatePermissions = await ValidatePermissions(
            guildId,
            logchannelId.Value,
            [
                ChannelPermission.ViewChannel,
                ChannelPermission.ReadMessageHistory,
                ChannelPermission.SendMessages,
                ChannelPermission.SendMessagesInThreads,
                ChannelPermission.EmbedLinks,
                ChannelPermission.AttachFiles,
            ],
            [
                GuildPermission.ViewAuditLog,
                GuildPermission.BanMembers,

                GuildPermission.ViewChannel,
                GuildPermission.ReadMessageHistory,
                GuildPermission.SendMessages,
                GuildPermission.SendMessagesInThreads,
                GuildPermission.EmbedLinks,
                GuildPermission.AttachFiles
            ]);
        if (validatePermissions.IsFailure)
        {
            embed.WithDescription(validatePermissions.Error.Item1);
            if (validatePermissions.Error.Item2 != null)
            {
                files.Add(CreateStringAttachment("exception.txt", validatePermissions.Error.Item2.ToString()));
            }
            await FollowupWithFilesAsync(files, embeds: [embed.Build()]);
            return;
        }

        validatePermissions.Value.AddEmbedFields(embed);

        await FollowupWithFilesAsync(files, embeds: [embed.Build()]);
    }
}
