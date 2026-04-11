using Discord;
using Microsoft.EntityFrameworkCore;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Data.Models.GuildApproval;

namespace XeniaDiscord.Common.Services;

partial class GuildApprovalService
{
    public async Task<ApproveUserResult> ApproveUser(
        IGuildUser user,
        IUser doneByUser)
    {
        var guildIdStr = user.Guild.Id.ToString();
        var config = await _db.GuildApprovals.AsNoTracking().FirstOrDefaultAsync(e => e.GuildId == guildIdStr);

        if (config == null)
        {
            return new ApproveUserResult(ApproveUserResultKind.NotConfigured, user, doneByUser, null, null);
        }
        if (!config.Enabled)
        {
            return new ApproveUserResult(ApproveUserResultKind.NotEnabled, user, doneByUser, null, null);
        }

        var roleId = config.GetApprovedRoleId();
        if (!roleId.HasValue)
        {
            return new ApproveUserResult(ApproveUserResultKind.ApprovedRoleNotConfigured, user, doneByUser, null, null);
        }
        var role = await user.Guild.GetRoleAsync(roleId.Value);
        if (role == null)
        {
            return new ApproveUserResult(ApproveUserResultKind.ApprovedRoleMissing, user, doneByUser, roleId.Value, null);
        }

        var targetFormatted = user.Username + (string.IsNullOrEmpty(user.Discriminator.Trim('0')?.Trim()) ? "" : $"#{user.Discriminator}");
        var invokerFormatted = doneByUser.Username + (string.IsNullOrEmpty(doneByUser.Discriminator.Trim('0')?.Trim()) ? "" : $"#{doneByUser.Discriminator}");
        if (user.RoleIds.Contains(role.Id))
        {
            return new ApproveUserResult(ApproveUserResultKind.UserAlreadyApproved, user, doneByUser, roleId.Value, role);
        }
        else
        {
            await ExceptionHelper.RetryOnTimedOut(async () =>
            {
                await user.AddRoleAsync(role);
            });
            await using var db = _db.CreateSession();
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
                await db.GuildApprovalLogEvents.AddAsync(new GuildApprovalLogEventModel
                {
                    GuildId = guildIdStr,
                    UserId = user.Id.ToString(),
                    ApprovedByUserId = doneByUser.Id.ToString()
                });
                await db.SaveChangesAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }
        try
        {
            await SendGreeterMessage(user.Guild, user);
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Failed to send greeter message for user \"{targetFormatted}\" ({user.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id})");
        }
        try
        {
            await SendLogEvent(user.Guild, new EmbedBuilder()
                .WithTitle("Approval - Approved User")
                .WithDescription($"{user.Mention} ({targetFormatted}, {user.Id})")
                .AddField("Approved By", $"{doneByUser.Mention} ({invokerFormatted}, {doneByUser.Id})")
                .WithColor(Color.Blue)
                .WithCurrentTimestamp());
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Failed to send log message for guild \"{user.Guild.Name}\" ({user.Guild.Id}) about {invokerFormatted} ({doneByUser.Id}) approving user {targetFormatted} ({user.Id})");
        }
        return new ApproveUserResult(ApproveUserResultKind.Success, user, doneByUser, roleId.Value, role);
    }

    public class ApproveUserResult
    {
        public ApproveUserResult(
            ApproveUserResultKind kind,
            IGuildUser user,
            IUser invokedByUser,
            ulong? approvedRoleId,
            IRole? approvedRole)
        {
            Kind = kind;
            User = user;
            InvokedByUser = invokedByUser;
            ApprovedRoleId = approvedRoleId;
            ApprovedRole = approvedRole;
        }

        public ApproveUserResultKind Kind { get; }
        public IGuildUser User { get; }
        public IUser InvokedByUser { get; }
        public ulong? ApprovedRoleId { get; }
        public IRole? ApprovedRole { get; }

        public bool IsSuccess
            => Kind == ApproveUserResultKind.Success
            || Kind == ApproveUserResultKind.UserAlreadyApproved;

        public string FormatForEmbed()
        {
            var targetFormatted = User.Username + (string.IsNullOrEmpty(User.Discriminator.Trim('0')?.Trim()) ? "" : $"#{User.Discriminator}");
            var invokerFormatted = InvokedByUser.Username + (string.IsNullOrEmpty(InvokedByUser.Discriminator.Trim('0')?.Trim()) ? "" : $"#{InvokedByUser.Discriminator}");
            switch (Kind)
            {
                case ApproveUserResultKind.Success:
                    return "Successfully approved user.";
                case ApproveUserResultKind.UserAlreadyApproved:
                    return "User has already been approved!\n"
                        + $"-# user: `{targetFormatted}` ({User.Id})"
                        + (ApprovedRole != null ? $"\n-# role: {ApprovedRole.Mention}" : string.Empty);
                case ApproveUserResultKind.NotConfigured:
                    return "Approval system has not been configured.";
                case ApproveUserResultKind.NotEnabled:
                    return "Approval system is not enabled.";
                case ApproveUserResultKind.ApprovedRoleMissing:
                    return "Could not find \"Approved Role\". It might need re-configuring with `/approval-admin set-approved-role`";
                case ApproveUserResultKind.ApprovedRoleNotConfigured:
                    return "\"Approved Role\" has not been configured.\nThis can be done with `/approval-admin set-approved-role`";
                case ApproveUserResultKind.FailedToGiveRole:
                    if (ApprovedRole != null)
                        return $"Failed to give role {ApprovedRole.Mention} to user {User.Mention} ({User.Username})";
                    return $"Failed to give \"Approved\" role to user {User.Mention} ({User.Username})";
                default:
                    return Kind.ToString();
            }
        }
    }

    public enum ApproveUserResultKind
    {
        Success,
        UserAlreadyApproved,
        NotConfigured,
        NotEnabled,
        ApprovedRoleMissing,
        ApprovedRoleNotConfigured,
        FailedToGiveRole,
    }
}