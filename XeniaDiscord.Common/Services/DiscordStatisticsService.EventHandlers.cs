using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Text;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Data.Models;

namespace XeniaDiscord.Common.Services;

partial class DiscordStatisticsService
{
    private record SlashCommandInteractionInfo(
        string? GuildName,
        string? GuildId,
        string? ChannelName,
        string? ChannelId,
        string AuthorUsername,
        string AuthorId,
        string? InteractionGroup,
        string InteractionName,
        string InteractionId);

    private async Task<SlashCommandInteractionInfo> GetInfo(SocketSlashCommand interaction)
    {
        var guild = interaction.GuildId.HasValue ? _client.GetGuild(interaction.GuildId.Value) : null;
        var usernameFormatted = interaction.User.Username;
        if (!string.IsNullOrEmpty(interaction.User.Discriminator?.Trim('0')))
            usernameFormatted += $"#{interaction.User.Discriminator}";

        var guildIdStr = interaction.GuildId?.ToString();
        var guildName = guildIdStr;
        if (guildIdStr != null)
        {
            guildName = guild?.Name
                ?? await _db.GuildPartialSnapshots.AsNoTracking()
                    .Where(e => e.GuildId == guildIdStr)
                    .OrderByDescending(e => e.Timestamp)
                    .Select(e => e.Name)
                    .FirstOrDefaultAsync();
        }

        var channelIdStr = interaction.ChannelId?.ToString();
        string? channelName = null;
        if (channelIdStr != null && interaction.Channel != null)
        {
            channelName = interaction.Channel.Name;
        }

        var interactionNameBuilder = new StringBuilder(interaction.Data.Name);
        var interactionGroup = string.Empty;

        if (interaction.Data is IApplicationCommandInteractionData data)
        {
            var any = false;
            foreach (var opt in data.Options)
            {
                any = true;
                interactionNameBuilder.Append($" {opt.Name}");
            }
            if (any)
            {
                interactionGroup = data.Name;
            }
        }

        return new SlashCommandInteractionInfo(
            guildName,
            guildIdStr,
            channelName,
            channelIdStr,
            usernameFormatted,
            interaction.User.Id.ToString(),
            string.IsNullOrEmpty(interactionGroup) ? string.Empty : interactionGroup,
            interactionNameBuilder.ToString(),
            interaction.Id.ToString());
    }

    private Task HandleMessageReceived(SocketMessage message)
    {
        var trans = SentryHelper.CreateTransaction();
        try
        {
            var usernameFormatted = message.Author.Username;
            if (!string.IsNullOrEmpty(message.Author.Discriminator?.Trim('0')))
                usernameFormatted += $"#{message.Author.Discriminator}";
            var labels = new[]
            {
                string.Empty,                   // guild_id
                string.Empty,                   // guild_name
                string.Empty,                   // channel_id
                string.Empty,                   // channel_name
                message.Author.Id.ToString(),   // author_id
                usernameFormatted               // author_name
            };
            if (message.Channel is SocketGuildChannel guildChannel)
            {
                labels[0] = guildChannel.Guild.Id.ToString();
                labels[1] = guildChannel.Guild.Name;
            }
            if (message.Channel != null)
            {
                labels[2] = message.Channel.Id.ToString();
                labels[3] = message.Channel.Name;
            }

            _statMessages.WithLabels(labels).Inc();
            trans.Finish();
        }
        catch (Exception ex)
        {
            trans.Finish(ex);
            _log.Error(ex, "Failed to make metrics");
        }

        return Task.CompletedTask;
    }
    private async Task HandleSlashCommandExecuted(SocketSlashCommand interaction)
    {
        var info = await GetInfo(interaction);

        _statInteractions.WithLabels(
            info.GuildName ?? "",
            info.GuildId ?? "",
            info.AuthorUsername,
            info.AuthorId,
            info.ChannelName ?? "",
            info.ChannelId ?? "",
            info.InteractionGroup ?? "",
            info.InteractionName,
            info.InteractionId
        ).Inc();

        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            if (await db.InteractionStatistics.AnyAsync(e
                => e.InteractionGroup == info.InteractionGroup
                && e.InteractionName == info.InteractionName
                && e.GuildId == info.GuildId
                && e.ChannelId == info.ChannelId
                && e.UserId == info.AuthorId))
            {
                if (info.GuildId == null && info.ChannelId == null)
                {
                    await db.Database.ExecuteSqlAsync(
                        $"UPDATE public.\"Statistics_Interactions\" SET \"Count\" = \"Count\" + 1 WHERE \"InteractionGroup\" = {info.InteractionGroup} AND \"InteractionName\" = {info.InteractionName} AND \"GuildId\" IS NULL AND \"ChannelId\" IS NULL AND \"UserId\" = {info.AuthorId};");
                }
                else if (info.GuildId == null && info.ChannelId != null)
                {
                    await db.Database.ExecuteSqlAsync(
                        $"UPDATE public.\"Statistics_Interactions\" SET \"Count\" = \"Count\" + 1 WHERE \"InteractionGroup\" = {info.InteractionGroup} AND \"InteractionName\" = {info.InteractionName} AND \"GuildId\" = {info.GuildId} AND \"ChannelId\" IS NULL AND \"UserId\" = {info.AuthorId};");
                }
                else if (info.GuildId != null && info.ChannelId == null)
                {
                    await db.Database.ExecuteSqlAsync(
                        $"UPDATE public.\"Statistics_Interactions\" SET \"Count\" = \"Count\" + 1 WHERE \"InteractionGroup\" = {info.InteractionGroup} AND \"InteractionName\" = {info.InteractionName} AND \"GuildId\" IS NULL AND \"ChannelId\" = {info.ChannelId} AND \"UserId\" = {info.AuthorId};");
                }
                else
                {
                    await db.Database.ExecuteSqlAsync(
                        $"UPDATE public.\"Statistics_Interactions\" SET \"Count\" = \"Count\" + 1 WHERE \"InteractionGroup\" = {info.InteractionGroup} AND \"InteractionName\" = {info.InteractionName} AND \"GuildId\" = {info.GuildId} AND \"ChannelId\" = {info.ChannelId} AND \"UserId\" = {info.AuthorId};");
                }
            }
            else
            {
                await db.InteractionStatistics.AddAsync(new InteractionStatisticModel()
                {
                    InteractionGroup = info.InteractionGroup,
                    InteractionName = info.InteractionName,
                    GuildId = info.GuildId,
                    ChannelId = info.ChannelId,
                    UserId = info.AuthorId,
                    Count = 1
                });
            }
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
        }
    }
}