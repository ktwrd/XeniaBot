using Discord;
using Discord.Interactions;
using XeniaDiscord.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Common.Repositories;
using XeniaDiscord.Data.Models.BanSync;
using System.Text;
using XeniaBot.Shared.Config;

namespace XeniaDiscord.Shared.Interactions.Modules;

[Group("bansync", "Sync Bans between servers")]
public class BanSyncModule : InteractionModuleBase
{
    private readonly BanSyncService _bs;
    private readonly BanSyncRepository _bsRepo;
    private readonly XeniaConfig _config;
    public BanSyncModule(IServiceProvider services)
    {
        _config = services.GetRequiredService<XeniaConfig>();
        _bs = services.GetRequiredService<BanSyncService>();
        _bsRepo = services.GetRequiredService<BanSyncRepository>();
    }
    [SlashCommand("userinfo", "Get ban sync details about user")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task UserDetails(IUser user)
    {
        await DeferAsync();
        var data = await _bsRepo.GetAllForUser(user, limit: 25);
        var count = await _bsRepo.GetCountForUser(user);

        if (data.Count == 0)
        {
            await Context.Interaction.FollowupAsync(embed: new EmbedBuilder()
            {
                Description = $"No bans found for <@{user.Id}> ({user.Id}, {user.Username})",
                Color = Color.Orange
            }.Build());
            return;
        }

        var embed = await _bs.GenerateEmbed(data, count);
        await Context.Interaction.FollowupAsync(embed: embed.Build());
    }

    [SlashCommand("setchannel", "Set the log channel where ban notifications get sent.")]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    public async Task SetChannel(
        [Summary(description: "Channel where BanSync notifications will be sent to.")]
        [ChannelTypes(ChannelType.Text)]
        ITextChannel logChannel)
    {
        try
        {
            await _bs.UpdateLogChannel(Context.Guild, logChannel, Context.User);
            await Context.Interaction.RespondAsync($"Updated Log Channel to <#{logChannel.Id}>");
        }
        catch (Exception ex)
        {
            await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                .WithTitle("Failed to set Log Channel")
                .WithColor(Color.Red)
                .WithDescription(ex.Message.Substring(0, 2000)).Build());
        }
    }

    [SlashCommand("request", "Request for this guild to have Ban Sync support")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task RequestGuild()
    {
        var kind = await _bs.GetGuildKind(Context.Guild.Id);
        var embed = new EmbedBuilder()
        {
            Title = "Request BanSync Access",
            Color = Color.Red
        }.WithCurrentTimestamp();

        switch (kind)
        {
            case BanSyncService.BanSyncGuildKind.LogChannelMissing:
                embed.Color = Color.Red;
                embed.Description = "You must set a log channel with `/bansync setchannel`.";
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
            case BanSyncService.BanSyncGuildKind.LogChannelCannotAccess:
                embed.Color = Color.Red;
                embed.Description = string.Join("\n",
                    "Xenia is unable to access the log channel that was set.",
                    "Please double-check the permissions in that channel.");
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
        }

        if (kind != BanSyncService.BanSyncGuildKind.Valid)
        {
            embed.Description = "Your server doesn't meet the requirements";
            embed.AddField("Reason", $"`{kind}`", true);
            await Context.Interaction.RespondAsync(embed: embed.Build());
            return;
        }
        
        BanSyncGuildModel? guildConfig;
        
        try
        {
            guildConfig = await _bs.RequestGuildEnable(Context.Guild.Id);
        }
        catch (Exception ex)
        {
            embed.Description = $"Failed to request guild BanSync support.\n```\n{ex.Message}\n```";
            await Context.Interaction.RespondAsync(embed: embed.Build());
            return;
        }
        
        if (guildConfig.State == BanSyncGuildState.PendingRequest)
        {
            embed.Color = Color.Green;
            embed.Description = "Your guild is under review for Ban Sync to be enabled.";
            await Context.Interaction.RespondAsync(embed: embed.Build());
            return;
        }

        embed.Description = $"Failed to request BanSync for this guild.\n`{guildConfig.State}`";
        // TODO add public-facing reason for denial
        // if (guildConfig.State == BanSyncGuildState.Blacklisted || guildConfig.State == BanSyncGuildState.RequestDenied)
        // {
        //     embed.AddField("Reason", $"```\n{guildConfig.Inter}\n```", true);
        // }
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }

    [SlashCommand("setguildstate", "Set state field of guild")]
    public async Task SetGuildState(ulong guildId, BanSyncGuildState state, string reason = "")
    {
        if (!_config.Discord.SuperuserIds.Contains(Context.Interaction.User.Id))
        {
            await Context.Interaction.RespondAsync("Not Authorized", ephemeral: true);
            return;
        }

        var targetGuild = await Context.Client.GetGuildAsync(guildId);
        if (targetGuild == null)
        {
            await Context.Interaction.RespondAsync($"Guild `{guildId}` not found (GetGuildAsync responded with null)");
            return;
        }

        try
        {
            await _bs.SetGuildState(guildId, state, reason, updatedBy: Context.Interaction.User);
        }
        catch (Exception ex)
        {
            await Context.Interaction.RespondWithFileAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(ex.ToString())),
                "exception.txt",

                embed: new EmbedBuilder()
                .WithTitle("Failed to set Guild State")
                .WithColor(Color.Red)
                .WithDescription(ex.Message.Substring(0, 2000)).Build());
            return;
        }
        await Context.Interaction.RespondAsync($"Set state of `{targetGuild.Name}` to `{state}`", ephemeral: true);
    }
}
