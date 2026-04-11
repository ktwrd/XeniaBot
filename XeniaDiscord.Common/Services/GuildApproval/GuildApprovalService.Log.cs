using Discord;
using Microsoft.EntityFrameworkCore;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.GuildApproval;

namespace XeniaDiscord.Common.Services;

partial class GuildApprovalService
{
    #region Action - Set Log Channel
    public async Task<SetLogChannelResult> SetLogChannel(IGuild guild, ITextChannel channel, IUser? doneByUser = null)
    {
        var ourMember = await guild.GetCurrentUserAsync();
        var ourChannelPermissions = ourMember.GetPermissions(channel);
        var ourChannelPermissionsList = ourChannelPermissions.ToList();
        var missingPermissions = RequiredChannelPermissions.Where(e => !ourChannelPermissionsList.Contains(e)).ToArray();
        if (missingPermissions.Length > 0)
        {
            return new SetLogChannelResult(
                SetLogChannelResultKind.MissingPermissions,
                missingPermissions,
                channel,
                guild);
        }


        ulong? previousChannelId = null;
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {

            var guildIdStr = guild.Id.ToString();
            if (await db.GuildApprovals.AnyAsync(e => e.GuildId == guildIdStr))
            {
                var previousChannelIdStr = await db.GuildApprovals
                    .Where(e => e.GuildId == guildIdStr)
                    .Select(e => e.LogChannelId)
                    .FirstOrDefaultAsync();
                previousChannelId = previousChannelIdStr.ParseULong(false);

                await db.GuildApprovals.Where(e => e.GuildId == guildIdStr)
                    .ExecuteUpdateAsync(e => e.SetProperty(p => p.LogChannelId, channel.Id.ToString()));
            }
            else
            {
                await db.GuildApprovals.AddAsync(new GuildApprovalModel
                {
                    GuildId = guildIdStr,
                    LogChannelId = channel.Id.ToString()
                });
            }

            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }

        try
        {
            if (previousChannelId.HasValue)
            {
                var logEmbed = new EmbedBuilder()
                    .WithTitle("Approval - Update \"Log Channel\"")
                    .WithDescription(
                        string.Join("\n",
                            $"From: <#{previousChannelId.Value}>",
                            $"To: <#{channel.Id}>"))
                    .WithColor(Color.Blue)
                    .WithCurrentTimestamp();
                if (doneByUser != null)
                {
                    var fmt = doneByUser.Username + (string.IsNullOrEmpty(doneByUser.Discriminator.Trim('0')) ? "" : $"#{doneByUser.Discriminator}");
                    logEmbed.AddField(
                        "Actioned By",
                        string.Join("\n",
                            "{doneByUser.Mention}",
                            "`{fmt}`"));
                }
                await SendLogEvent(guild, logEmbed);
            }
        }
        catch (Exception ex)
        {
            var msg = $"Failed to send log message in Guild \"{guild.Name}\" ({guild.Id}) for \"Greeter Channel\" role being updated to \"{channel.Name}\" ({channel.Id})";
            _log.Warn(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithGuild(guild)
                .WithNotes(msg));
        }

        return new SetLogChannelResult(
            SetLogChannelResultKind.Success,
            [],
            channel,
            guild);
    }

    public class SetLogChannelResult
    {
        public SetLogChannelResult(
            SetLogChannelResultKind kind,
            ChannelPermission[] missingPermissions,
            ITextChannel targetChannel,
            IGuild targetGuild)
        {
            Kind = kind;
            MissingPermissions = missingPermissions;
            TargetChannel = targetChannel;
            TargetGuild = targetGuild;
        }
        public SetLogChannelResultKind Kind { get; }
        public ChannelPermission[] MissingPermissions { get; }
        public ITextChannel TargetChannel { get; }
        public IGuild TargetGuild { get; }

        public bool IsSuccess => Kind == SetLogChannelResultKind.Success;
        public bool IsFailure => !IsSuccess;

        public string FormatForEmbed()
        {
            var fmtChannel = $"<#{TargetChannel.Id}>";
            switch (Kind)
            {
                case SetLogChannelResultKind.Success:
                    return $"Successfully updated Log Channel to: {fmtChannel}";
                case SetLogChannelResultKind.MissingPermissions:
                    return string.Join("\n",
                        $"Cannot update Log Channel to {fmtChannel} - Missing one or more permissions:",
                        "```",
                        string.Join("\n", MissingPermissions.Select(e => e.ToString())),
                        "```",
                        "Please make sure that you give those permissions directly to Xenia, and not a role that Xenia has so permission validation can work more reliably.");
                default:
                    return Kind.ToString();
            }
        }
    }
    public enum SetLogChannelResultKind
    {
        Success,
        MissingPermissions
    }
    #endregion

    #region Send Log Event
    private async Task SendLogEvent(
        IGuild guild,
        EmbedBuilder embed)
    {
        var guildIdStr = guild.Id.ToString();
        var logChannelStr = await _db.GuildApprovals.AsNoTracking()
            .Where(e => e.GuildId == guildIdStr && e.Enabled)
            .Select(e => e.LogChannelId)
            .FirstOrDefaultAsync();
        
        var channelId = logChannelStr.ParseULong(false);
        if (!channelId.HasValue) return;

        var channel = await guild.GetTextChannelAsync(channelId.Value);
        if (channel == null)
        {
            _log.Warn($"Could not find channel {channelId} in Guild \"{guild.Name}\" ({guild.Id})");
            return;
        }
        try
        {
            await channel.SendMessageAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            var msg = $"Failed to send log message in Channel \"{channel.Name}\" ({channel.Id}) in Guild \"{guild.Name}\" ({guild.Id})";
            _log.Warn(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithGuild(guild)
                .WithChannel(channel)
                .AddSerializedAttachment("embedBuilder.json", embed));
        }
    }
    #endregion
}