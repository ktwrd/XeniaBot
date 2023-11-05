using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;

namespace XeniaBot.Core.Modules;


public class ModerationModule : InteractionModuleBase
{
    /// <exception cref="NonfatalException">When failed to fetch client/guild/member. This should be displayed to the user as well as the developers.</exception>
    private async Task<SocketGuildUser?> SafelyFetchUser(ulong userId)
    {
        var embed = DiscordHelper.BaseEmbed()
            .WithColor(Color.Red)
            .WithTitle("Failed to safely fetch user");
        try
        {
            DiscordSocketClient client = (DiscordSocketClient)Context.Client;
            if (client == null)
            {

                throw new NonfatalException("Failed to fetch discord client");
            }

            SocketGuild? guild = client.GetGuild(Context.Guild.Id);
            if (guild == null)
            {
                throw new NonfatalException($"Failed to fetch guild ({Context.Guild.Id})");
            }

            SocketGuildUser? member = guild.GetUser(userId);
            if (member == null)
            {
                throw new NonfatalException($"Member not found ({userId})");
            }

            return member;
        }
        catch (NonfatalException e)
        {
            await Context.Interaction.RespondAsync(
                embed: embed.WithDescription(e.Message).Build(),
                ephemeral: true);
            return null;
        }
        catch (Exception e)
        {
            embed.WithDescription($"Fatal Error! This has been reported to the developers.\n```\n{e.Message}\n```");
            await Context.Interaction.RespondAsync(
                embed: embed.Build(),
                ephemeral: true);
            await DiscordHelper.ReportError(e, Context);
        }
        return null;
    }

    [SlashCommand("warn", "Warn member")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task WarnMember(SocketGuildUser user, string reason)
    {
        SocketGuildUser? member = await SafelyFetchUser(user.Id);
        if (member == null)
            return;

        var embed = DiscordHelper.BaseEmbed().WithTitle("Warn Member");
        try
        {
            var controller = Program.Services.GetRequiredService<GuildWarnItemConfigController>();
            var data = new GuildWarnItemModel()
            {
                GuildId = user.Guild.Id,
                TargetUserId = user.Id,
                ActionedUserId = Context.User.Id,
                CreatedAtTimestamp  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Description = reason
            };
            await controller.Add(data);
            embed.WithDescription($"Warned member <@{user.Id}>. Warn Id `{data.WarnId}`.");
            if (Program.ConfigData.HasDashboard)
            {
                embed.Description +=
                    $"\n[View on Dashboard]({Program.ConfigData.DashboardLocation}/Warn/Info/{data.WarnId})";
            }

            embed.WithColor(Color.Blue);

            await RespondAsync(embed: embed.Build());
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
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
            await DiscordHelper.ReportError(e, Context);
            return;
        }
    }
    
    [SlashCommand("kick", "Kick member from server")]
    [RequireUserPermission(GuildPermission.KickMembers)]
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
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
            await DiscordHelper.ReportError(e, Context);
        }

        embed.WithDescription($"Successfully kicked `{user.Username}#{user.DiscriminatorValue}`")
             .WithColor(Color.Blue);
        if (reason != null)
            embed.AddField("Reason", reason);
        
        await Context.Interaction.RespondAsync(
            embed: embed.Build(),
            ephemeral: true);
    }

    [SlashCommand("ban", "Ban member from server")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task BanMember(SocketGuildUser user, string? reason = null, 
        [Discord.Interactions.Summary(description: "How many days of messages should be deleted when this member is banned")]
        int pruneDays=0)
    {
        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Ban Member");
        if (pruneDays < 0)
        {
            embed.WithColor(Color.Red).WithDescription("Parameter `pruneDays` cannot be less than `0`");
            await Context.Interaction.RespondAsync(
                embed: embed.Build(),
                ephemeral: true);
            return;
        }
        
        SocketGuildUser? member = await SafelyFetchUser(user.Id);
        if (member == null)
            return;

        try
        {
            await member.BanAsync(pruneDays, reason);
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
            await Context.Interaction.RespondAsync(
                embed: embed.Build(),
                ephemeral: true);
            await DiscordHelper.ReportError(e, Context);
            return;
        }

        string description = $"Successfully banned `{user.Username}#{user.DiscriminatorValue}`";
        if (pruneDays > 0)
        {
            description += $"\nRemoved all messages in the last {pruneDays} day" + (pruneDays > 1 ? "s" : "");
        }

        embed.WithDescription(description);
        embed.WithColor(Color.Blue);
        if (reason != null)
            embed.AddField("Reason", reason);
        
        await Context.Interaction.RespondAsync($"Banned member `{user.Username}#{user.DiscriminatorValue}`", ephemeral: true);
    }


    [SlashCommand("purge", "Purge messages")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task PurgeMessages(
        int count,
        [ChannelTypes(ChannelType.Text)] IChannel? channel = null)
    {
        var client = Program.Services.GetRequiredService<DiscordSocketClient>();
        var guild = client.GetGuild(Context.Guild.Id);
        
        // Use Context.Channel when no channel given.
        SocketTextChannel targetChannel = guild.GetTextChannel(channel?.Id ?? Context.Channel.Id);
        SocketTextChannel currentChannel = guild.GetTextChannel(Context.Channel.Id);

        // Let the user know that it might take a while.
        
        await Context.Interaction.RespondAsync($"Calculating (this may take a while)");
        List<IMessage> messageList = new List<IMessage>();
        try
        {
            messageList = await FetchRecursiveMessages(targetChannel, count);
        }
        catch (Exception e)
        {
            
            await currentChannel.SendMessageAsync(embed: DiscordHelper.BaseEmbed()
                .WithTitle("Failed to fetch message list")
                .WithDescription($"```\n{e.Message}\n```")
                .WithColor(Color.Red).Build());
            await DiscordHelper.ReportError(e, Context);
            return;
        }

        if (messageList.Count < 1)
        {
            await currentChannel.SendMessageAsync(embed: DiscordHelper.BaseEmbed()
                .WithTitle("No messages found")
                .WithColor(Color.Red).Build());
            return;
        }

        var notifyMessage = await currentChannel.SendMessageAsync(embed: DiscordHelper.BaseEmbed()
            .WithTitle($"Purged {messageList.Count} messages")
            .WithDescription(string.Join("\n", new string[]
            {
                $"```",
                $"From: {messageList.Last().Timestamp}",
                $"To  : {messageList.First().Timestamp}",
                $"```"
            })).Build());
        try
        {
            await targetChannel.DeleteMessagesAsync(messageList);
        }
        catch (Exception e)
        {
            await notifyMessage.ReplyAsync(embed: DiscordHelper.BaseEmbed()
                .WithTitle("Failed to delete messages")
                .WithDescription($"```\n{e.Message}\n```")
                .WithColor(Color.Red).Build());
            await DiscordHelper.ReportError(e, Context);
        }
    }

    private async Task<List<IMessage>> FetchRecursiveMessages(SocketTextChannel channel, int? max = null)
    {
        List<IMessage> messageList = new List<IMessage>();
        IEnumerable<IMessage> messages = await channel.GetMessagesAsync(Math.Min(max ?? 100, 300)).FlattenAsync();
        
        bool allowContinue = true;
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
                    messages = await channel.GetMessagesAsync(fromMessageId: item.Id, Direction.Before).FlattenAsync();
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