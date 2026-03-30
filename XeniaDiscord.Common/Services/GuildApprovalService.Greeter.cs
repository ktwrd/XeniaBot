using System.Text;
using Discord;
using Microsoft.EntityFrameworkCore;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data.Models.GuildApproval;

namespace XeniaDiscord.Common.Services;

partial class GuildApprovalService
{
    #region Action - Set Greeter Channel
    public async Task<SetGreeterChannelResult> SetGreeterChannel(
        IGuild guild,
        ITextChannel channel,
        IUser? doneByUser = null)
    {
        var ourMember = await guild.GetCurrentUserAsync();
        var ourChannelPermissions = ourMember.GetPermissions(channel);
        var ourChannelPermissionsList = ourChannelPermissions.ToList();
        var missingPermissions = RequiredChannelPermissions.Where(e => !ourChannelPermissionsList.Contains(e)).ToArray();
        if (missingPermissions.Length > 0)
        {
            return new SetGreeterChannelResult(
                SetGreeterChannelResultKind.MissingPermissions,
                missingPermissions,
                channel,
                guild);
        }

        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var guildIdStr = guild.Id.ToString();
            if (await db.GuildApprovals.AnyAsync(e => e.GuildId == guildIdStr))
            {
                await db.GuildApprovals.Where(e => e.GuildId == guildIdStr)
                    .ExecuteUpdateAsync(e => e.SetProperty(p => p.GreeterChannelId, channel.Id.ToString()));
            }
            else
            {
                await db.GuildApprovals.AddAsync(new GuildApprovalModel
                {
                    GuildId = guildIdStr,
                    GreeterChannelId = channel.Id.ToString()
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
            var logEmbed = new EmbedBuilder()
                .WithTitle("Approval - Update \"Greeter Channel\"")
                .WithDescription($"Updated `Greeter Channel` to {channel.Mention}")
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
        catch (Exception ex)
        {
            var msg = $"Failed to send log message in Guild \"{guild.Name}\" ({guild.Id}) for \"Greeter Channel\" role being updated to \"{channel.Name}\" ({channel.Id})";
            _log.Warn(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithGuild(guild)
                .WithNotes(msg));
        }

        return new SetGreeterChannelResult(
            SetGreeterChannelResultKind.Success,
            [],
            channel,
            guild);
    }
    public class SetGreeterChannelResult
    {
        public SetGreeterChannelResult(
            SetGreeterChannelResultKind kind,
            ChannelPermission[] missingPermissions,
            ITextChannel targetChannel,
            IGuild targetGuild)
        {
            Kind = kind;
            MissingPermissions = missingPermissions;
            TargetChannel = targetChannel;
            TargetGuild = targetGuild;
        }
        public SetGreeterChannelResultKind Kind { get; }
        public ChannelPermission[] MissingPermissions { get; }
        public ITextChannel TargetChannel { get; }
        public IGuild TargetGuild { get; }

        public bool IsSuccess => Kind == SetGreeterChannelResultKind.Success;
        public bool IsFailure => !IsSuccess;

        public string FormatForEmbed()
        {
            var fmtChannel = $"<#{TargetChannel.Id}>";
            switch (Kind)
            {
                case SetGreeterChannelResultKind.Success:
                    return $"Successfully updated Greeter Channel to: {fmtChannel}";
                case SetGreeterChannelResultKind.MissingPermissions:
                    return string.Join("\n",
                        $"Cannot update Greeter Channel to {fmtChannel} - Missing one or more permissions:",
                        "```",
                        string.Join("\n", MissingPermissions.Select(e => e.ToString())),
                        "```",
                        "Make sure that you give those permissions directly to Xenia, and not a role that Xenia has so permission validation can work more reliably.");
                default:
                    return Kind.ToString();
            }
        }
    }
    public enum SetGreeterChannelResultKind
    {
        Success,
        MissingPermissions,
    }
    #endregion

    #region Send Greeter Message
    public async Task SendGreeterMessage(
        IGuild guild,
        IGuildUser newUser)
    {
        if (!await IsGreeterEnabled(guild.Id)) return;

        var guildIdStr = guild.Id.ToString();
        var config = await _db.GuildApprovals.AsNoTracking().FirstOrDefaultAsync(e => e.GuildId == guildIdStr);
        await SendGreeterMessage(config, guild, newUser);
    }

    private async Task SendGreeterMessage(
        GuildApprovalModel? config,
        IGuild guild,
        IGuildUser newUser)
    {
        if (config?.Enabled != true || config?.EnableGreeter != true) return;

        var channelId = config.GetGreeterChannelId();
        if (!channelId.HasValue)
        {
            await SendLogEvent(guild,
                new EmbedBuilder()
                    .WithTitle("Approval - Send Greeter Message")
                    .WithDescription(string.Join("\n",
                        "Greeter is enabled, but there is no greeter channel configured.",
                        "This can be fixed with `/approval-admin set-greeter-channel`"))
                    .WithColor(Color.Red));
            return;
        }
        if (string.IsNullOrEmpty(config.GreeterMessageTemplate?.Trim()))
        {
            await SendLogEvent(guild,
                new EmbedBuilder()
                    .WithTitle("Approval - Send Greeter Message")
                    .WithDescription(string.Join("\n",
                        "Missing Greeter message template!",
                        "This can be fixed with `/approval-admin set-greeter-message`"))
                    .WithColor(Color.Red));
            return;
        }

        ITextChannel? textChannel = null;

        await ExceptionHelper.RetryOnTimedOut(async () =>
        {
            textChannel = await guild.GetTextChannelAsync(channelId.Value);
        });
        if (textChannel == null)
        {
            throw new InvalidOperationException($"Could not find channel: {channelId.Value}");
        }

        
        var messageContent = "";
        if ((!config.GreeterMessageTemplate.Contains("{user_mention}", StringComparison.OrdinalIgnoreCase) || config.GreeterAsEmbed)
            && config.GreeterMentionUser)
        {
            messageContent = newUser.Mention + "\n";
        }

        var formattedContent = FormatMessageTemplate(config.GreeterMessageTemplate, guild, newUser);
        await ExceptionHelper.RetryOnTimedOut(async () =>
        {
            if (config.GreeterAsEmbed)
            {
                await textChannel.SendMessageAsync(
                    messageContent,
                    embed: new EmbedBuilder().WithDescription(formattedContent).Build());
            }
            else
            {
                await textChannel.SendMessageAsync(messageContent + formattedContent);
            }
        });
        async Task SubmitError(Exception ex)
        {
            var msgSuffix = $"in guild \"{guild.Name}\" ({guild.Id}) for user \"{newUser.Username}#{newUser.Discriminator}\" ({newUser.Id})";
            var msg = textChannel == null
                ? $"Failed to get channel \"{channelId}\" {msgSuffix}"
                : $"Failed to send greeter message in channel \"{textChannel.Name}\" ({textChannel.Id}) {msgSuffix}";
            _log.Error(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithNotes(msg)
                .WithException(ex)
                .WithGuild(guild)
                .WithUser(newUser)
                .WithChannel(textChannel));
        }
    }

    public static string FormatMessageTemplate(
        string template,
        IGuild guild,
        IGuildUser newUser)
    {
        var username = newUser.Username;
        if (!string.IsNullOrEmpty(newUser.Discriminator.Trim('0')))
            username += $"#{newUser.Discriminator}";
        var userDisplayName = username;
        if (!string.IsNullOrEmpty(newUser.DisplayName))
            userDisplayName = newUser.DisplayName;
        else if (!string.IsNullOrEmpty(newUser.GlobalName))
            userDisplayName = newUser.GlobalName;

        var messageContent = new StringBuilder();
        var cmdBuf = new StringBuilder();
        var useCmdBuf = false;
        foreach (var c in template)
        {
            switch (c)
            {
                case '}' when useCmdBuf:
                    switch (cmdBuf.ToString().Trim().ToLower())
                    {
                        case "user_id":
                            messageContent.Append(newUser.Id.ToString());
                            break;
                        case "user_mention":
                            messageContent.Append(newUser.Mention);
                            break;
                        case "user_username":
                            messageContent.Append(username);
                            break;
                        case "user_display_name":
                            messageContent.Append(userDisplayName);
                            break;
                        case "guild_name":
                            messageContent.Append(guild.Name);
                            break;
                    }
                    cmdBuf.Clear();
                    useCmdBuf = false;
                    break;
                case '{' when !useCmdBuf:
                    useCmdBuf = true;
                    break;
                default:
                    if (useCmdBuf)
                    {
                        cmdBuf.Append(c);
                    }
                    else
                    {
                        messageContent.Append(c);
                    }
                    break;
            }
        }
        return messageContent.ToString();
    }
    #endregion
}