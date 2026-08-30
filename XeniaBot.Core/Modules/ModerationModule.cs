using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Services;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Modules;

[CommandContextType(InteractionContextType.Guild)]
public class ModerationModule : InteractionModuleBase
{

    private readonly WarnStrikeService _warnStrikeService;
    private readonly WarnService _warnService;
    private readonly ConfigData _configData;
    private readonly ErrorReportService _err;
    private readonly DiscordShardedClient _client;

    public ModerationModule(IServiceProvider services)
    {
        _warnStrikeService = services.GetRequiredService<WarnStrikeService>();
        _warnService = services.GetRequiredService<WarnService>();
        _configData = services.GetRequiredService<ConfigData>();
        _err = services.GetRequiredService<ErrorReportService>();
        _client = services.GetRequiredService<DiscordShardedClient>();
    }

    /// <exception cref="NonfatalException">When failed to fetch client/guild/member. This should be displayed to the user as well as the developers.</exception>
    private async Task<SocketGuildUser?> SafelyFetchUser(ulong userId)
    {
        var embed = DiscordHelper.BaseEmbed()
            .WithColor(Color.Red)
            .WithTitle("Failed to safely fetch user");
        try
        {
            var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(Context.Guild.Id));
            if (guild == null)
            {
                throw new NonfatalException($"Failed to fetch guild ({Context.Guild.Id})");
            }

            var member = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(userId));
            if (member == null)
            {
                throw new NonfatalException($"Member not found ({userId})");
            }

            return member;
        }
        catch (NonfatalException e)
        {
            await ExceptionHelper.RetryOnTimedOut(async () => await Context.Interaction.RespondAsync(embed: embed.WithDescription(e.Message).Build(), ephemeral: true));
            return null;
        }
        catch (Exception e)
        {
            embed.WithDescription($"Fatal Error! This has been reported to the developers.\n```\n{e.Message}\n```");
            await ExceptionHelper.RetryOnTimedOut(async () => await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true));
            await DiscordHelper.ReportError(e, Context);
        }
        return null;
    }

    [SlashCommand("warn", "Warn member")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task WarnMember(IUser user, string reason)
    {
        await DeferAsync();

        var embed = DiscordHelper.BaseEmbed().WithTitle("Warn Member");
        try
        {
            var data = await _warnService.CreateWarnAsync(
                Context.Guild,
                user, 
                Context.User, 
                reason);
            embed.WithDescription($"Warned member <@{user.Id}>");
            embed.WithFooter($"{data.WarnId}");

            var warnStrikeConfig = await _warnStrikeService.GetStrikeConfig(Context.Guild.Id);
            var (reachedWarnLimit, activeWarns) = await _warnStrikeService.UserReachedWarnLimit(Context.Guild.Id, user.Id);
            if (reachedWarnLimit)
            {
                embed.AddField(
                    "User Reached Warn Limit",
                    $"Has {activeWarns!.Count} active warns (limit is {warnStrikeConfig.MaxStrike})\nAction immediately or ignore this message.");
            }
            
            if (_configData.HasDashboard)
            {
                embed.Description +=
                    $"\n[View on Dashboard]({Program.Core.Config.Data.DashboardUrl}/Warn/Info/{data.WarnId})";
            }

            embed.WithColor(reachedWarnLimit ? Color.Red : Color.Blue);

            await ExceptionHelper.RetryOnTimedOut(async () => await FollowupAsync(embed: embed.Build()));
        }
        catch (Exception e)
        {
            embed.WithDescription(string.Join("\n", new string[]
            {
                "Failed to warn member, this has been reported to the developers",
                "```",
                e.Message,
                "```"
            }));
            embed.WithColor(Color.Red);
            await ExceptionHelper.RetryOnTimedOut(async () => await FollowupAsync(embed: embed.Build()));
            await DiscordHelper.ReportError(e, Context);
            return;
        }
    }

    [SlashCommand("warns", "Get all warns for user. Only top 10")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task MemberWarns(SocketGuildUser user)
    {
        await DeferAsync();
        var embed = DiscordHelper.BaseEmbed().WithTitle("Member Warns");
        try
        {
            var warnStrikeConfig = await _warnStrikeService.GetStrikeConfig(Context.Guild.Id);
            var (reachedWarnLimit, data) = await _warnStrikeService.UserReachedWarnLimit(Context.Guild.Id, user.Id);

            if (data?.Count < 1)
            {
                embed.WithDescription($"No active warns found for <@{user.Id}>")
                    .WithColor(Color.Orange);
                await ExceptionHelper.RetryOnTimedOut(async () => await FollowupAsync(embed: embed.Build()));
                return;
            }

            string encaseWithDashboardUrl(GuildWarnItemModel item)
            {
                if (_configData.HasDashboard)
                {
                    return $" ([View on Dashboard]({_configData.DashboardUrl}/Warn/Info/{item.WarnId}))";
                }
                return "";
            }
        
            for (int i = 0; i < Math.Min(data!.Count, 10); i++)
            {
                var item = data[i];
            
                embed.AddField(
                    DateTimeOffset.FromUnixTimeMilliseconds(item.CreatedAtTimestamp).ToString("yyyy MMMM dd, h:mm:ss tt"),
                    string.Join("\n",
                        $"Created by <@{item.ActionedUserId}>" + encaseWithDashboardUrl(item),
                        "```",
                        item.Description.Length < 1
                            ? "<no description>"
                            : item.Description.Length > 500
                                ? item.Description[500..] + "..."
                                : item.Description,
                        "```"
                    ), true);
            }

            embed.WithDescription($"{data.Count} records for <@{user.Id}> " + (data.Count > 10 ? $" (10 shown)" : ""))
                .WithColor(reachedWarnLimit ? Color.Red : Color.Blue);
            if (reachedWarnLimit)
            {
                embed.Description +=
                    $"\n\n**Member has {data.Count} active warn{(data.Count > 1 ? 's'.ToString() : string.Empty)}**, but the limit is {warnStrikeConfig.MaxStrike}!\n" +
                    $"Action immediately or ignore this notification.";
            }
            await ExceptionHelper.RetryOnTimedOut(async () => await FollowupAsync(embed: embed.Build()));
        }
        catch (Exception ex)
        {
            embed.WithDescription(string.Join("\n", new string[]
            {
                "Failed to get warns for user.",
                "```",
                ex.Message,
                "```"
            }));
            embed.WithColor(Color.Red);
            await ExceptionHelper.RetryOnTimedOut(async () => await FollowupAsync(embed: embed.Build()));
            await DiscordHelper.ReportError(ex, Context);
        }
    }
    
    [SlashCommand("kick", "Kick member from server")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    [RequireBotPermission(GuildPermission.KickMembers | GuildPermission.ViewAuditLog)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task KickMember(SocketGuildUser user, string? reason = null)
    {
        SocketGuildUser? member = await SafelyFetchUser(user.Id);
        if (member == null)
            return;

        var embed = DiscordHelper.BaseEmbed().WithTitle("Kick Member");
        
        try
        {
            await member.KickAsync(reason);
        }
        catch (Exception e)
        {
            embed.WithDescription(string.Join("\n", new string[]
            {
                "Failed to kick member, this has been reported to the developers",
                "```",
                e.Message,
                "```"
            }));
            embed.WithColor(Color.Red);
            await ExceptionHelper.RetryOnTimedOut(async () => await Context.Interaction.RespondAsync(embed: embed.Build()));
            await DiscordHelper.ReportError(e, Context);
            return;
        }

        embed.WithDescription($"Successfully kicked `{user.Username}#{user.DiscriminatorValue}`")
             .WithColor(Color.Blue);
        if (reason != null)
            embed.AddField("Reason", reason);
        
        await ExceptionHelper.RetryOnTimedOut(async () => await Context.Interaction.RespondAsync(embed: embed.Build()));
    }

    [SlashCommand("ban", "Ban member from server")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers | GuildPermission.ViewAuditLog)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task BanMember(SocketGuildUser user, string? reason = null, 
        [Summary(description: "How many days of messages should be deleted when this member is banned")]
        int pruneDays=0)
    {
        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Ban Member");
        if (pruneDays < 0)
        {
            embed.WithColor(Color.Red).WithDescription("Parameter `pruneDays` cannot be less than `0`");
            await ExceptionHelper.RetryOnTimedOut(async () => await Context.Interaction.RespondAsync(embed: embed.Build()));
            return;
        }

        try
        {
            await Context.Guild.AddBanAsync(user.Id, pruneDays, reason);
        }
        catch (Exception e)
        {
            embed.WithDescription(string.Join("\n", new string[]
            {
                "Failed to ban member, this has been reported to the developers.",
                "```",
                e.Message,
                "```"
            }));
            embed.WithColor(Color.Red);
            await ExceptionHelper.RetryOnTimedOut(async () => await Context.Interaction.RespondAsync(embed: embed.Build()));
            await DiscordHelper.ReportError(e, Context);
            return;
        }

        string description = $"Successfully banned `{user.Username}#{user.DiscriminatorValue} ({user.Id})`";
        if (pruneDays > 0)
        {
            description += $"\nRemoved all messages in the last {pruneDays} day" + (pruneDays > 1 ? "s" : "");
        }

        embed.WithDescription(description);
        embed.WithColor(Color.Blue);
        if (reason != null)
            embed.AddField("Reason", reason);
        
        await ExceptionHelper.RetryOnTimedOut(async () => await Context.Interaction.RespondAsync($"Banned member `{user.Username}#{user.DiscriminatorValue}`"));
    }


    [SlashCommand("purge", "Purge messages")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task PurgeMessages(
        int count,
        [ChannelTypes(ChannelType.Text)] IChannel? channel = null)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(Context.Guild.Id));
        
        // Use Context.Channel when no channel given.
        var targetChannel = ExceptionHelper.RetryOnTimedOut(() => guild.GetTextChannel(channel?.Id ?? Context.Channel.Id));
        var currentChannel = ExceptionHelper.RetryOnTimedOut(() => guild.GetTextChannel(Context.Channel.Id));

        // Let the user know that it might take a while.
        
        await Context.Interaction.RespondAsync($"Calculating (this may take a while)");
        var messageList = new List<IMessage>();
        try
        {
            messageList = await FetchRecursiveMessages(targetChannel, count);
        }
        catch (Exception e)
        {
            var errEmbed = DiscordHelper.BaseEmbed()
                .WithTitle("Failed to fetch message list")
                .WithDescription($"```\n{e.Message}\n```")
                .WithColor(Color.Red);
            await ExceptionHelper.RetryOnTimedOut(async () => await currentChannel.SendMessageAsync(embed: errEmbed.Build()));
            await DiscordHelper.ReportError(e, Context);
            return;
        }

        if (messageList.Count < 1)
        {
            var embedNone = DiscordHelper.BaseEmbed()
                .WithTitle("No messages found")
                .WithColor(Color.Red);
            await ExceptionHelper.RetryOnTimedOut(async () =>
                await currentChannel.SendMessageAsync(embed: embedNone.Build()));
            return;
        }

        var embed = DiscordHelper.BaseEmbed()
            .WithTitle($"Purged {messageList.Count} messages")
            .WithDescription(string.Join("\n", new string[]
            {
                $"```",
                $"From: {messageList.Last().Timestamp}",
                $"To  : {messageList.First().Timestamp}",
                $"```"
            }));
        var notifyMessage = await ExceptionHelper.RetryOnTimedOut(async () => await currentChannel.SendMessageAsync(embed: embed.Build()));
        try
        {
            await targetChannel.DeleteMessagesAsync(messageList);
        }
        catch (Exception e)
        {
            var errEmbed = DiscordHelper.BaseEmbed()
                .WithTitle("Failed to delete messages")
                .WithDescription($"```\n{e.Message}\n```")
                .WithColor(Color.Red);
            await ExceptionHelper.RetryOnTimedOut(async () => await notifyMessage.ReplyAsync(embed: errEmbed.Build()));
            await DiscordHelper.ReportError(e, Context);
        }
    }

    private static async Task<List<IMessage>> FetchRecursiveMessages(SocketTextChannel channel, int? max = null)
    {
        var messages = await channel.GetMessagesAsync(Math.Min(max ?? 100, 300)).FlattenAsync();
        if (messages == null) return [];
        
        var messageList = new List<IMessage>();

        var allowContinue = true;
        while (allowContinue)
        {
            foreach (var item in messages)
            {
                // Once we've reached our limit, we break the loop
                if (max != null && messageList.Count + 1 > max)
                {
                    allowContinue = false;
                    break;
                }
                
                messageList.Add(item);
                var isLast = messages.Last().Id == item.Id;
                if (isLast)
                {
                    messages = await ExceptionHelper.RetryOnTimedOut(async () => await channel.GetMessagesAsync(fromMessageId: item.Id, Direction.Before).FlattenAsync());
                    var last = messages.LastOrDefault();
                    if (last == null || last?.Id == item.Id || last?.Timestamp > item.Timestamp)
                    {
                        allowContinue = false;
                    }
                }
            }
        }

        return messageList;
    }
}