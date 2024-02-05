using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Services.Wrappers;
using XeniaBot.Core.Helpers;
using XeniaBot.Data.Models;
using XeniaBot.Data.Models.Archival;
using XeniaBot.Data.Repositories;
using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class ServerLogService : BaseController
{
    private readonly ServerLogRepository _config;
    private readonly DiscordSocketClient _discord;
    private readonly DiscordCacheService _discordCache;
    public ServerLogService(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<ServerLogRepository>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _discordCache = services.GetRequiredService<DiscordCacheService>();
    }

    public override Task InitializeAsync()
    {
        _discord.UserJoined += Event_UserJoined;
        _discord.UserLeft += Event_UserLeave;
        _discord.UserBanned += Event_UserBan;
        _discord.UserUnbanned += Event_UserBanRemove;

        _discord.MessageDeleted += Event_MessageDelete;
        _discordCache.MessageChange += DiscordCacheMessageChangeUpdate;

        return Task.CompletedTask;
    }

    
    private async void DiscordCacheMessageChangeUpdate(MessageChangeType type, CacheMessageModel current, CacheMessageModel? previous)
    {
        try
        {
            if (type != MessageChangeType.Update)
                return;

            var previousContent = previous?.Content ?? "";
            var currentContent = current.Content ?? "";
            if (previousContent == currentContent)
                return;

            var author = _discord.GetUser(current.AuthorId);
            if (author == null)
                return;

            var diffContent = string.Join("\n", SGeneralHelper.GenerateDifference(previousContent ?? "", currentContent ?? ""));

            var embed = DiscordHelper.BaseEmbed()
                .WithTitle("Message Edited")
                .WithDescription(string.Join("\n", new string[]
                {
                    $"From: `{author.Username}#{author.Discriminator}`",
                    $"ID: `{current.AuthorId}`"
                }))
                .WithColor(new Color(255, 255, 255))
                .WithUrl($"https://discord.com/channels/{current.GuildId}/{current.ChannelId}/{current.Snowflake}")
                .WithThumbnailUrl(author.GetAvatarUrl());
            if (diffContent.Length > 1000)
                await EventHandle(current.GuildId, (v => v.MessageEditChannel), embed, diffContent, "diff.txt");
            else
            {
                embed.AddField("Difference",
                    string.Join("\n", new string[]
                    {
                        "```",
                        diffContent,
                        "```"
                    }));
                await EventHandle(current.GuildId, (v => v.MessageEditChannel), embed);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to handle MessageChangeUpdate event!!\n{ex}");
            var author = _discord.GetUser(current.AuthorId);
            var guild = _discord.GetGuild(current.GuildId);
            var channel = _discord.GetChannel(current.ChannelId) as IMessageChannel;
            IMessage? msg = null;
            if (channel != null)
                msg = await channel.GetMessageAsync(current.Snowflake);
            await DiscordHelper.ReportError(ex, author, guild, channel, msg);
        }
    }
    internal async Task EventHandle(ulong serverId, Func<ServerLogModel, ulong?> selectChannel, EmbedBuilder embed, string? attachmentContent = null, string? attachmentName = null)
    {
        var data = await _config.Get(serverId);

        // Server not setup for logs, aborting.
        if (data == null)
            return;

        var targetChannel = selectChannel(data) ?? data.DefaultLogChannel;
        if (targetChannel == null || targetChannel == 0)
            return;

        var server = _discord.GetGuild(serverId);
        var logChannel = server.GetTextChannel(targetChannel);
        if (logChannel == null)
            return;

        try
        {
            if (attachmentContent == null)
            {
                await logChannel.SendMessageAsync(embed: embed.Build());
                return;
            }

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(attachmentContent));
            
            await logChannel.SendFileAsync(ms, attachmentName ?? "content.txt", embed: embed.Build());
        }
        catch (Exception e)
        {
            if (e.Message.Contains("Missing Access") || e.Message.Contains("50001") || e.Message.Contains("50013"))
            {
                await server.Owner.SendMessageAsync(
                    string.Join(
                        "\n", new string[]
                        {
                            "Heya!", "",
                            $"Xenia does not have access to send log events in a channel in the server `{server.Name}`, which you own.",
                            "",
                            "In order for the logging feature to work, make sure that Xenia has access to the following permissions.",
                            "- View Channel",
                            "- Send Messages",
                            "- Embed Links",
                            "", $"Channel affected: https://discord.com/channels/{server.Id}/{targetChannel}"
                        }));
            }
            throw;
        }
    }

    #region User Events
    private async Task Event_UserJoined(SocketGuildUser user)
    {
        var userSafe = user.Username.Replace("`", "\\`");
        var embed = new EmbedBuilder()
            .WithTitle("User Joined")
            .WithDescription($"<@{user.Id}>" + string.Join("\n", new string[]
            {
                "```",
                $"{userSafe}#{user.Discriminator}",
                $"ID: {user.Id}",
                "```",
            }))
            .AddField("Account Age", string.Join("\n", new string[]
            {
                TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                $"`{user.CreatedAt}`"
            }))
            .WithThumbnailUrl(user.GetAvatarUrl())
            .WithColor(Color.Green);

        await EventHandle(user.Guild.Id, (v) => v.MemberJoinChannel, embed);
    }
    private async Task Event_UserLeave(SocketGuild guild, SocketUser user)
    {
        var userSafe = user.Username.Replace("`", "\\`");
        var embed = new EmbedBuilder()
            .WithTitle("User Left")
            .WithDescription($"<@{user.Id}>" + string.Join("\n", new string[]
            {
                "```",
                $"{userSafe}#{user.Discriminator}",
                $"ID: {user.Id}",
                "```",
            }))
            .AddField("Account Age", string.Join("\n", new string[]
            {
                TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                $"`{user.CreatedAt}`"
            }))
            .WithThumbnailUrl(user.GetAvatarUrl())
            .WithColor(Color.Red);

        await EventHandle(guild.Id, (v) => v.MemberLeaveChannel, embed);
    }

    private async Task Event_UserBan(SocketUser user, SocketGuild guild)
    {
        try
        {
            var userSafe = user.Username.Replace("`", "\\`");
            var banDetails = await guild.GetBanAsync(user.Id);
            var reason = banDetails.Reason ?? "<Unknown Reason>";
            var embed = new EmbedBuilder()
                .WithTitle("User Banned")
                .WithDescription($"<@{user.Id}>" + string.Join("\n", new string[]
                {
                    "```",
                    $"{userSafe}#{user.Discriminator}",
                    $"ID: {user.Id}",
                    "```",
                }))
                .AddField("Account Age", string.Join("\n", new string[]
                {
                    TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                    $"`{user.CreatedAt}`"
                }))
                .AddField("Ban Reason", $"```\n{reason}\n```")
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithColor(Color.Red);

            await EventHandle(guild.Id, (v) => v.MemberBanChannel, embed);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to run.", ex);
            await DiscordHelper.ReportError(
                ex,
                $"Failed run ServerLogService.Event_UserBan.\nUser: {user} ({user.Id})\nGuild: {guild.Name} ({guild.Id})");
        }
    }
    private async Task Event_UserBanRemove(SocketUser user, SocketGuild guild)
    {
        try
        {
            
            var userSafe = user.Username.Replace("`", "\\`");
            var embed = new EmbedBuilder()
                .WithTitle("User Unbanned")
                .WithDescription($"<@{user.Id}>" + string.Join("\n", new string[]
                {
                    "```",
                    $"{userSafe}#{user.Discriminator}",
                    $"ID: {user.Id}",
                    "```",
                }))
                .AddField("Account Age", string.Join("\n", new string[]
                {
                    TimeHelper.SinceTimestamp(user.CreatedAt.ToUnixTimeMilliseconds()),
                    $"`{user.CreatedAt}`"
                }))
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithColor(Color.Red);

            await EventHandle(guild.Id, (v) => v.MemberBanChannel, embed);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to run.", ex);
            await DiscordHelper.ReportError(
                ex,
                $"Failed run ServerLogService.Event_UserBanRemove.\nUser: {user} ({user.Id})\nGuild: {guild.Name} ({guild.Id})");
        }
    }
    #endregion
    
    #region Message Events

    private async Task Event_MessageDelete(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
    {
        var socketChannel = channel.Value as SocketGuildChannel;
        if (socketChannel?.Guild == null)
            return;
        try
        {
            var funkyMessage = await _discordCache.CacheMessageConfig.GetLatest(message.Id);
        
            string messageContent = message.Value?.Content ?? funkyMessage?.Content ?? "";
            long timestamp = 
                message.Value?.CreatedAt.ToUnixTimeSeconds()
                ?? funkyMessage?.CreatedAt.ToUnixTimeSeconds()
                ?? 0;
            SocketUser? author = _discord.GetUser(message.Value?.Author.Id ?? funkyMessage?.AuthorId ?? 0);
            var embed = DiscordHelper.BaseEmbed()
                .WithTitle("Message Deleted")
                .WithDescription($"Deleted in <#{channel.Id}> at <t:{timestamp}:F>")
                .WithColor(Color.Orange);
            if (author != null)
                embed.WithThumbnailUrl(author.GetAvatarUrl());
        
            if (messageContent.Length is < 1000 and > 0)
                embed.AddField("Content", $"```\n{messageContent}\n```");
            else if (messageContent.Length > 1000)
            {
                embed.AddField("Content", "Attached to this message");
                await EventHandle(socketChannel.Guild.Id, (v) => v.MessageDeleteChannel, embed, messageContent, "content.txt");
                return;
            }
            await EventHandle(socketChannel.Guild.Id, (v) => v.MessageDeleteChannel, embed);
        }
        catch (Exception ex)
        {
            var msg = string.Join(
                    "\n", new string[]
                    {
                        "Failed run ServerLogService.Event_MessageDelete.", $"ChannelId: {socketChannel.Id}",
                        $"Guild: {socketChannel.Guild.Id} ({socketChannel.Guild.Name})",
                        $"MessageId: {message.Value?.Id ?? 0}"
                    });
            Log.Error(msg, ex);
            await DiscordHelper.ReportError(
                ex,
                msg);
        }
    }

    private async Task Event_MessageEdit(Cacheable<IMessage, ulong> previousMessage, SocketMessage currentMessage,
        IMessageChannel channel)
    {
        var socketChannel = channel as SocketGuildChannel;
        try
        {
            
            var previousContent = previousMessage.Value?.Content ?? "";
            var storedData = await _discordCache.CacheMessageConfig.GetLatest(currentMessage.Id);
            if (previousContent == currentMessage.Content)
                return;
            var diffContent = string.Join(
                "\n", SGeneralHelper.GenerateDifference(previousContent ?? "", currentMessage?.Content ?? ""));
            var embed = DiscordHelper.BaseEmbed()
                .WithTitle("Message Edited")
                .WithDescription($"From `{currentMessage.Author.Username}#{currentMessage.Author.Discriminator}`\nID: `{currentMessage.Author.Id}`")
                .WithColor(new Color(255, 255, 255))
                .WithUrl($"https://discord.com/channels/{socketChannel.Guild.Id}/{socketChannel.Id}/{currentMessage.Id}")
                .WithThumbnailUrl(currentMessage.Author.GetAvatarUrl());
            if (diffContent.Length < 1000)
            {
                embed
                    .AddField("Difference", string.Join("\n",
                        new string[]
                        {
                            "```",
                            diffContent,
                            "```",
                        }));
                await EventHandle(socketChannel.Guild.Id, (v) => v.MessageEditChannel, embed);
            }
            else
            {
                await EventHandle(socketChannel.Guild.Id, (v) => v.MessageEditChannel, embed, diffContent, "diff.txt");
            }
        }
        catch (Exception ex)
        {
            var msg = string.Join(
                "\n", new string[]
                {
                    "Failed run ServerLogService.Event_MessageDelete.", $"ChannelId: {socketChannel?.Id}",
                    $"Guild: {socketChannel?.Guild.Id} ({socketChannel?.Guild.Name})",
                    $"MessageId: {currentMessage?.Id ?? 0}"
                });
            Log.Error(msg, ex);
            await DiscordHelper.ReportError(
                ex,
                msg);
        }
    }
    #endregion
}