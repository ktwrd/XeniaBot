using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Discord.WebSocket;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Common.Services.BanSync;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Interactions.Modules;

[Group("bansync", "Receive ban notifications across guilds")]
[CommandContextType(InteractionContextType.Guild)]
public class BanSyncModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();

    private readonly ConfigData _config;
    private readonly ErrorReportService _err;

    private readonly BanSyncService _bansyncService;
    private readonly BanSyncGuildRepository _guildRepo;
    private readonly BanSyncRecordRepository _recordRepo;
    private readonly DiscordSocketClient _discordClient;

    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;

    public BanSyncModule(IServiceProvider services)
    {
        _config = services.GetRequiredService<ConfigData>();
        _err = services.GetRequiredService<ErrorReportService>();
        _discordClient = services.GetRequiredService<DiscordSocketClient>();

        _bansyncService = services.GetRequiredService<BanSyncService>();
        _guildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        _recordRepo = services.GetRequiredService<BanSyncRecordRepository>();

        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
    }

    [SlashCommand("refresh", "Refresh bans in this guild")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [UsedImplicitly]
    public async Task Refresh()
    {
        await DeferAsync();
        try
        {
            var guild = ExceptionHelper.RetryOnTimedOut(() => _discordClient.GetGuild(Context.Guild.Id));
            if (guild == null)
            {
                await FollowupAsync("Internal error (Guild not found)");
                return;
            }

            if (guild.MemberCount > 300)
            {
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("BanSync - Refresh")
                        .WithDescription(
                            "Your guild is too large to have BanSync records refreshed by a server admin.\n" +
                            $"Please join our [support server]({_config.SupportServerUrl}) to have a developer refresh them for you.")
                        .WithColor(Color.Red)
                        .WithCurrentTimestamp()
                        .Build());
                return;
            }
            await _bansyncService.RefreshBans(Context.Guild.Id);
            
            await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle("BanSync - Refresh")
                    .WithDescription("Bans were refreshed successfully.")
                    .WithColor(Color.Blue)
                    .WithCurrentTimestamp()
                    .Build());
        }
        catch (Exception ex)
        {
            var msg = $"Failed to refresh bans for Guild \"{Context.Guild.Name}\" ({Context.Guild.Id})";
            _log.Error(ex, msg);
            await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle("BanSync - Action Failed")
                    .WithDescription("Failed to refresh bans in this guild (this has been reported to the developers)")
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
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task UserDetails(
        [Summary(description: "User to get information about.")]
        IUser user)
    {
        try
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
                var embed = _bansyncService.GenerateEmbed(data);
                await Context.Interaction.FollowupAsync(embed: embed.Build());
            }
        }
        catch (Exception ex)
        {
            var msg = $"Failed to get user information for: {user.Id}";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithContext(Context));
            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle("BanSync - Get User Info")
                .WithDescription("Failed to get user information. This has been reported to the developers")
                .WithCurrentTimestamp()
                .WithColor(Color.Red)
                .Build());
        }
    }

    [SlashCommand("setchannel", "Set the log channel where ban notifications get sent.")]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task SetChannel(
        [Summary(description: "Channel where BanSync notifications will be sent to.")]
        [ChannelTypes(ChannelType.Text)]
        ITextChannel logChannel)
    {
        await DeferAsync();

        var selfMember = await ExceptionHelper.RetryOnTimedOut(async () => await Context.Guild.GetCurrentUserAsync());
        var selfPerms = selfMember.GetPermissions(logChannel);
        var missingPermissions = new List<string>(2);
        if (!selfPerms.SendMessages) missingPermissions.Add("Send Messages");
        if (!selfPerms.EmbedLinks) missingPermissions.Add("Embed Links");
        if (missingPermissions.Count > 0)
        {
            var embedResult = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .WithTitle("BanSync Set Channel - Missing Permissions")
                .WithDescription("Xenia is missing one or more permissions in " + logChannel.Mention + "\n" +
                                 string.Join("\n", missingPermissions.Select(e => $"- {e}")) + "\n\n" +
                                 "-# [See full list of required permissions](https://xenia.kate.pet/guide/required_permissions#content-bansync)");
            await Context.Interaction.FollowupAsync(embed: embedResult.Build());
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var data = await _guildRepo.GetAsync(db, Context.Guild.Id)
                ?? new(Context.Guild.Id);
            data.LogChannelId = logChannel.Id.ToString();
            await _guildRepo.InsertOrUpdate(db, data);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
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

        var successMsg = $"Updated Log Channel to {logChannel.Mention}";
        await Context.Interaction.FollowupAsync(successMsg);
    }
    
    [SlashCommand("setguildstate", "Set state field of guild")]
    [RequireDeveloper]
    [UsedImplicitly]
    public async Task SetGuildState(
        string guild,
        BanSyncGuildState state,
        string reason = "")
    {
        if (!_config.UserWhitelist.Contains(Context.User.Id))
        {
            await Context.Interaction.RespondAsync("Invalid permissions");
            return;
        }

        if (!ulong.TryParse(guild, out var guildId))
        {
            await Context.Interaction.RespondAsync($"Invalid Guild ID: {guildId}", ephemeral: true);
            return;
        }
        var targetGuild = await ExceptionHelper.RetryOnTimedOut(async () => await Context.Client.GetGuildAsync(guildId));
        if (targetGuild == null)
        {
            await Context.Interaction.RespondAsync($"Guild not found: `{guildId}`", ephemeral: true);
            return;
        }

        try
        {
            await _bansyncService.SetGuildState(guildId, state, reason);
            var n = targetGuild.Name.Replace("`", "'");
            await Context.Interaction.RespondAsync($"Set state of `{n}` to `{state}`");
        }
        catch (Exception ex)
        {
            var msg = $"Failed to set state to {state} for guild \"{targetGuild.Name}\" ({guildId})";
            _log.Error(ex, msg);
            await Context.Interaction.RespondAsync($"Failed to set guild state\n```\n{ex.Message}\n```", ephemeral: true);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithGuild(targetGuild)
                .WithContext(Context));
        }
    }

    [SlashCommand("request", "Request for this guild to have BanSync support")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task RequestGuild()
    {
        var kindRes = await _bansyncService.GetGuildKind(Context.Guild);

        var embed = new EmbedBuilder()
            .WithTitle("Request BanSync Access")
            .WithColor(Color.Red)
            .WithCurrentTimestamp();

        embed.Description = kindRes.FormatMessage(FormatMessageKind.MessageEmbed);
        embed.Color = kindRes.Success ? Color.Green : Color.Red;
        switch (kindRes.GuildKind)
        {
            case BanSyncGuildKind.PendingRequest:
                embed.Color = Color.Blue;
                break;
            case BanSyncGuildKind.LogChannelMissing or BanSyncGuildKind.LogChannelCannotAccess:
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        
        if (kindRes.GuildKind != BanSyncGuildKind.Valid)
        {
            if (string.IsNullOrEmpty(embed.Description))
            {
                embed.Description = "Unable to request for BanSync";
                embed.AddField("Reason", kindRes.GuildKind, true);
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
                .AddAttachment("bansyncGuildKind.txt", $"{kindRes.GuildKind}: {embed.Description}"));
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
        if (guildConfig.State is BanSyncGuildState.Blacklisted or BanSyncGuildState.RequestDenied &&
            !string.IsNullOrWhiteSpace(guildConfig.Notes))
        {
            var notes = guildConfig.Notes?.Trim();
            if (notes?.Length > 1021)
                notes = notes[..1021] + "...";
            embed.AddField("Reason", notes, true);
        }
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }
}
