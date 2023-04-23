using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Helpers;

namespace SkidBot.Core.Modules;


public class ModerationModule : InteractionModuleBase
{
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
    public async Task BanMember(SocketGuildUser user, string? reason = null, int pruneDays=0)
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
    private async Task<List<IMessage>> FetchRecursiveMessages(SocketTextChannel channel, int? max = null)
    {
        List<IMessage> messageList = new List<IMessage>();
        IEnumerable<IMessage> messages = await channel.GetMessagesAsync(Math.Min(max ?? 100, 100)).FlattenAsync();
        
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