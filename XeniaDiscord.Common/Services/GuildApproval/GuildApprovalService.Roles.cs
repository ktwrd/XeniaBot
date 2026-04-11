using System.Net;
using Discord;
using Microsoft.EntityFrameworkCore;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data.Models.GuildApproval;

namespace XeniaDiscord.Common.Services;

public partial class GuildApprovalService
{
    #region Action - Set "Approved" Role
    public async Task<SetApprovedRoleResult> SetApprovedRole(
        IGuild guild,
        IRole role,
        IUser? doneByUser = null)
    {
        var ourMember = await guild.GetCurrentUserAsync();
        var ourRoles = await Task.WhenAll(ourMember.RoleIds.Select(id => guild.GetRoleAsync(id)));
        var ourHighestRole = ourRoles.OrderByDescending(e => e.Position).FirstOrDefault();
        if (ourHighestRole == null || role.Position > ourHighestRole.Position)
        {
            return new SetApprovedRoleResult(
                SetApprovedRoleResultKind.UnableToGrantRole,
                role, guild, ourMember, ourHighestRole);
        }
        else if (!ourMember.GuildPermissions.ManageRoles)
        {
            return new SetApprovedRoleResult(
                SetApprovedRoleResultKind.MissingPermission_ManageRoles,
                role, guild, ourMember, ourHighestRole);
        }

        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var guildIdStr = guild.Id.ToString();
            var roleIdStr = role.Id.ToString();
            if (await db.GuildApprovals.AnyAsync(e => e.GuildId == guildIdStr))
            {
                await db.GuildApprovals.Where(e => e.GuildId == guildIdStr)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.ApprovedRoleId, roleIdStr));
            }
            else
            {
                await db.GuildApprovals.AddAsync(new GuildApprovalModel
                {
                    GuildId = guildIdStr,
                    ApprovedRoleId = roleIdStr,
                    Enabled = true,
                });
            }
            await db.SaveChangesAsync();
            await trans.CommitAsync();

            _log.Debug($"Set \"Approved\" role to \"{role.Name}\" ({role.Id}) for Guild \"{guild.Name}\" ({guild.Id})");
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }

        try
        {
            var logEmbed = new EmbedBuilder()
                .WithTitle("Approval - Update \"Approved Role\"")
                .WithDescription(
                    "Role was updated to: {role.Mention}\n"
                    + string.Join("\n",
                        "```",
                        $"Id: {role.Id}",
                        $"Name: {role.Name}",
                        $"Position: {role.Position}",
                        "```"))
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
            var msg = $"Failed to send log message in Guild \"{guild.Name}\" ({guild.Id}) for \"Approved\" role being updated to: {role.Id}";
            _log.Warn(ex, msg);
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithGuild(guild)
                .WithRole(role)
                .WithNotes(msg));
        }

        return new SetApprovedRoleResult(
            SetApprovedRoleResultKind.Success,
            role, guild, ourMember, ourHighestRole);
    }

    public class SetApprovedRoleResult
    {
        public SetApprovedRoleResult(
            SetApprovedRoleResultKind kind,
            IRole role,
            IGuild guild,
            IGuildUser ourGuildUser,
            IRole? ourHighestRole)
        {
            Kind = kind;
            TargetRole = role;
            Guild = guild;
            OurGuildUser = ourGuildUser;
            OurHighestRole = ourHighestRole;
        }
        public SetApprovedRoleResultKind Kind { get; }
        public IRole TargetRole { get; }
        public IGuildUser OurGuildUser { get; }
        public IRole? OurHighestRole { get; }
        public IGuild Guild { get; }

        public bool IsSuccess => Kind == SetApprovedRoleResultKind.Success;
        public bool IsFailure => !IsSuccess;

        public string FormatForEmbed()
        {
            var fmtRole = $"<@&{TargetRole.Id}>";
            var fmtOurHighest = OurHighestRole == null ? "which we don't have" : $"<@&{OurHighestRole.Id}>";
            switch (Kind)
            {
                case SetApprovedRoleResultKind.Success:
                    return $"Successfully updated \"Approved\" role to: {fmtRole}";
                case SetApprovedRoleResultKind.MissingPermission_ManageRoles:
                    return $"Xenia is missing the permission <code>Manage Roles</code>";
                case SetApprovedRoleResultKind.UnableToGrantRole:
                    return $"Unable to use the role {fmtRole} since it's higher than Xenia's highest role, {fmtOurHighest}.\n"
                         + $"This can be fixed by putting Xenia in a role that's higher than {fmtRole} in the \"Roles\" page of your Guild settings, or making the Xenia role higher than it.";
                default:
                    return Kind.ToString();
            }
        }
        public string FormatForWeb()
        {
            var roleName = WebUtility.HtmlEncode(TargetRole.Name);
            var ourHighestRoleName = OurHighestRole == null ? null : WebUtility.HtmlEncode(OurHighestRole.Name);

            var fmtRole = $"<code alt=\"{TargetRole.Id}\">@{roleName}</code>";
            var fmtOurHighest = OurHighestRole == null ? "which we don't have" : $"<code alt=\"{OurHighestRole?.Id}\">@{ourHighestRoleName}</code>";
            switch (Kind)
            {
                case SetApprovedRoleResultKind.Success:
                    return $"Successfully updated \"Approved\" role to: {fmtRole}";
                case SetApprovedRoleResultKind.MissingPermission_ManageRoles:
                    return $"Xenia is missing the permission <code>Manage Roles</code>";
                case SetApprovedRoleResultKind.UnableToGrantRole:
                    return $"Unable to use the role {fmtRole} since it's higher than Xenia's highest role, {fmtOurHighest}.\n"
                         + $"This can be fixed by putting Xenia in a role that's higher than {fmtRole} in the \"Roles\" page of your Guild settings, or making the Xenia role higher than it.";
                default:
                    return Kind.ToString();
            }
        }
    }

    public enum SetApprovedRoleResultKind
    {
        Success,
        MissingPermission_ManageRoles,
        UnableToGrantRole
    }
    #endregion
}