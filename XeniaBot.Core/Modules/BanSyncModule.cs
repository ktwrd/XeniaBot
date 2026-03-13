using Discord;
using Discord.Interactions;
using kate.shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.Core.Modules;

[Group("bansync", "Sync Bans between servers")]
public class BanSyncModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();

    private readonly ConfigData _config;
    private readonly ErrorReportService _err;

    private readonly BanSyncService _bansyncService;
    private readonly BanSyncGuildRepository _guildRepo;
    private readonly BanSyncRecordRepository _recordRepo;

    private readonly XeniaDbContext _db;

    public BanSyncModule(IServiceProvider services)
    {
        _config = services.GetRequiredService<ConfigData>();
        _err = services.GetRequiredService<ErrorReportService>();
     
        _bansyncService = services.GetRequiredService<BanSyncService>();
        _guildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        _recordRepo = services.GetRequiredService<BanSyncRecordRepository>();

        _db = services.GetRequiredService<XeniaDbContext>();
    }

    [SlashCommand("refresh", "Refresh bans in this guild")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task Refresh()
    {
        await DeferAsync();
        try
        {
            await _bansyncService.RefreshBans(Context.Guild.Id);
            
            await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle("BanSync - Refresh")
                    .WithDescription("Bans were refreshed successfully.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp()
                    .Build());
        }
        catch (Exception ex)
        {
            var msg = $"Failed to refresh bans for Guild {Context.Guild.Name} ({Context.Guild.Id})";
            _log.Error(ex, msg);
            await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle("BanSync - Action Failed")
                    .WithDescription("Failed to refresh bans in this guild")
                    .AddField("Error Message", ex.Message[..Math.Min(ex.Message.Length, 1000)])
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp()
                    .Build());
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithContext(Context));
        }
    }
    
    [SlashCommand("userinfo", "Get ban sync details about user")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task UserDetails(
        [Summary(description: "User to get information about.")]
        IUser user)
    {
        await Context.Interaction.DeferAsync();
        var data = await _recordRepo.GetInfoEnumerable(user.Id);

        if (data.Count == 0)
        {
            await Context.Interaction.FollowupAsync(
                embed: new EmbedBuilder()
                    .WithDescription($"No bans found for <@{user.Id}> ({user.Username}, {user.Id})")
                    .WithColor(Color.Orange)
                    .Build());
        }
        else
        {
            var embed = await _bansyncService.GenerateEmbed(data);
            await Context.Interaction.FollowupAsync(embed: embed.Build());
        }
    }

    [SlashCommand("setchannel", "Set the log channel where ban notifications get sent.")]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    public async Task SetChannel(
        [Summary(description: "Channel where BanSync notifications will be sent to.")]
        [ChannelTypes(ChannelType.Text)]
        ITextChannel logChannel)
    {
        await DeferAsync();
        try
        {
            var data = await _guildRepo.GetAsync(Context.Guild.Id)
                ?? new(Context.Guild.Id);
            data.LogChannelId = logChannel.Id.ToString();
            await _guildRepo.InsertOrUpdate(data);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to update log channel to {logChannel.Id} for guild \"{Context.Guild.Name}\" ({Context.Guild.Id})";
            _log.Error(ex, msg);
            try
            {
                await _err.Submit(new ErrorReportBuilder()
                    .WithException(ex)
                    .WithNotes(msg)
                    .WithChannel(logChannel)
                    .WithContext(Context));
            }
            catch { }

            await Context.Interaction.FollowupAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription($"Failed to update log channel! ({ex.GetType().Namespace}.{ex.GetType().Name})")
                .WithFooter("Don't worry, this issue has been reported to the developers.")
                .Build());
            return;
        }
        await Context.Interaction.FollowupAsync($"Updated Log Channel to <#{logChannel.Id}>");
    }
    
    [SlashCommand("setguildstate", "Set state field of guild")]
    public async Task SetGuildState(string guild, BanSyncGuildState state, string reason = "")
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions.");
            return;
        }
        ulong guildId = 0;
        try
        {
            guildId = ulong.Parse(guild);
        }
        catch (Exception ex)
        {
            await Context.Interaction.RespondAsync($"Failed to parse guildId\n\n{ex.Message}", ephemeral: true);
            return;
        }
        var targetGuild = await Context.Client.GetGuildAsync(guildId);
        if (targetGuild == null)
        {
            await Context.Interaction.RespondAsync($"Guild `{guildId}` not found", ephemeral: true);
            return;
        }

        try
        {
            await _bansyncService.SetGuildState(guildId, state, reason);
            await Context.Interaction.RespondAsync($"Set state of `{targetGuild.Name}` to `{state}`");
        }
        catch (Exception ex)
        {
            var msg = $"Failed to set state to {state} for guild {targetGuild.Name} ({guildId})";
            _log.Error(ex, msg);
            await Context.Interaction.RespondAsync($"Failed to set guild state\n```\n{ex.Message}\n```", ephemeral: true);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithGuild(targetGuild)
                .WithContext(Context));
        }
    }

    [SlashCommand("request", "Request for this guild to have Ban Sync support")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task RequestGuild()
    {
        var kind = await _bansyncService.GetGuildKind(Context.Guild.Id);

        var embed = new EmbedBuilder()
            .WithTitle("Request BanSync Access")
            .WithColor(Color.Red)
            .WithCurrentTimestamp();

        switch (kind)
        {
            case BanSyncGuildKind.LogChannelMissing:
                embed.Color = Color.Red;
                embed.Description = "You must set a log channel with `/bansync setchannel`.";
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
            case BanSyncGuildKind.LogChannelCannotAccess:
                embed.Color = Color.Red;
                embed.Description = string.Join("\n",
                    "Xenia is unable to access the log channel that was set.",
                    "Please double-check the permissions in that channel.");
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
        }

        var guildIdStr = Context.Interaction.GuildId!.ToString();
        var logChannelIdStr = await _db.BanSyncGuilds
            .AsNoTracking()
            .Where(e => e.GuildId == guildIdStr)
            .Select(e => e.LogChannelId)
            .FirstOrDefaultAsync();
        var logChannelId = logChannelIdStr?.ParseULong(false);
        var logChannelSuffixStr = logChannelId.HasValue ? $": <#{logChannelId}>" : ".";
        embed.Description = kind switch
        {
            BanSyncGuildKind.NotEnoughMembers => "Your server doesn't have enough members. It needs at least `35`.",
            BanSyncGuildKind.Blacklisted => "Your server is blacklisted from the BanSync feature.",
            BanSyncGuildKind.MissingBanMembersPermission
                => "Xenia is missing the \"Ban Members\" permission.\n"
                + "**This is required** to view who's been banned in your server.\n"
                + "-# [Source](https://docs.discord.com/developers/resources/guild#get-guild-bans)",
            BanSyncGuildKind.LogChannelCannotAccess
                => $"Xenia cannot access or view your BanSync log channel{logChannelSuffixStr}",
            BanSyncGuildKind.LogChannelCannotSendMessages
                => $"Xenia doesn't have permission to send messages in your BanSync log channel{logChannelSuffixStr}",
            BanSyncGuildKind.LogChannelCannotSendEmbeds
                => $"Xenia doesn't have permission to send embeds in your BanSync log channel{logChannelSuffixStr}",
            _ => kind.ToDescriptionString(kind.ToString())
        };


        if (kind != BanSyncGuildKind.Valid)
        {
            if (string.IsNullOrEmpty(embed.Description))
            {
                embed.Description = "Unable to request for BanSync";
                embed.AddField("Reason", kind, true);
            }
            await Context.Interaction.RespondAsync(embed: embed.Build());
            return;
        }
        
        BanSyncGuildModel? guildConfig;
        
        try
        {
            guildConfig = await _bansyncService.RequestGuildEnable(Context.Guild.Id);
        }
        catch (Exception ex)
        {
            var extype = ex.GetType();
            embed.Description = $"Failed to request guild BanSync support\n-# {extype.Namespace}.{extype.Name}: {ex.Message}";
            await Context.Interaction.RespondAsync(embed: embed.Build());
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithContext(Context)
                .AddAttachment("bansyncGuild-kind.txt", $"{kind}: {kind.ToDescriptionString(kind.ToString())}"));
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
        if (guildConfig.State == BanSyncGuildState.Blacklisted ||
            guildConfig.State == BanSyncGuildState.RequestDenied)
        {
            var notes = guildConfig.Notes?.Trim();
            if (notes?.Length > 1021)
                notes = notes[..1021] + "...";
            embed.AddField("Reason", notes, true);
        }
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }
}
