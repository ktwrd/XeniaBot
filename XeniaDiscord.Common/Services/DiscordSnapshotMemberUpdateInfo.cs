using Discord;
using System.Text;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Common.Services;

public class DiscordSnapshotMemberUpdateInfo
{
    public required IReadOnlyCollection<(ulong RoleId, GuildRoleSnapshotModel? Role)> RolesAdded { get; init; }
    public required IReadOnlyCollection<(ulong RoleId, GuildRoleSnapshotModel? Role)> RolesRemoved { get; init; }
    public required IReadOnlyCollection<GuildPermission> PermissionsAdded { get; init; }
    public required IReadOnlyCollection<GuildPermission> PermissionsRemoved { get; init; }
    public required GuildMemberSnapshotModel? SnapshotBefore { get; init; }
    public required GuildMemberSnapshotModel Snapshot { get; init; }

    public bool AnyUpdates
        => RolesAdded.Count > 0
        || RolesRemoved.Count > 0
        || AnyPermissions;
    public bool AnyPermissions
        => PermissionsAdded.Count > 0
        || PermissionsRemoved.Count > 0;

    public EmbedBuilder WithRolesAdded(EmbedBuilder embed, List<FileAttachment> attachments)
    {
        if (RolesAdded.Count < 1) return embed;

        var added = string.Join("\n", RolesAdded.Select(e => $"<@&{e.RoleId}>"));
        var diffAdd = string.Join("\n", RolesAdded.Select(role => $"{role.RoleId} - {role.Role?.Name}"));
        if (added.Length < 1024)
        {
            embed.AddField("Roles Added", added, true);
        }
        else if (added.Length >= 1024)
        {
            embed.AddField("Roles Added", "-# Attached as `roles-added.txt`", true);
            attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(diffAdd)), "roles-added.txt"));
        }
        return embed;
    }
    public EmbedBuilder WithRolesRemoved(EmbedBuilder embed, List<FileAttachment> attachments)
    {
        if (RolesRemoved.Count < 1) return embed;

        var content = string.Join("\n", RolesRemoved.Select(e => $"<@&{e.RoleId}>"));
        var fileContent = string.Join("\n", RolesRemoved.Select(role => $"{role.RoleId} - {role.Role?.Name}"));
        if (content.Length < 1024)
        {
            embed.AddField("Roles Removed", content, true);
        }
        else if (content.Length >= 1024)
        {
            embed.AddField("Roles Removed", "-# Attached as `roles-removed.txt`", true);
            attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(fileContent)), "roles-removed.txt"));
        }
        return embed;
    }
    public EmbedBuilder WithPermissionsAdded(EmbedBuilder embed, List<FileAttachment> attachments)
    {
        if (PermissionsAdded.Count < 1) return embed;
        var content = string.Join("\n", PermissionsAdded.Select(e => e.ToString()));
        if (content.Length >= 1024)
        {
            embed.AddField(
                $"Permissions Added ({PermissionsAdded.Count})",
                "-# Attached as `permissions-added.txt`",
                true);
            attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(content)), "permissions-added.txt"));
        }
        else if (content.Length > 0)
        {
            embed.AddField(
                $"Permissions Added ({PermissionsAdded.Count})",
                content,
                true);
        }
        return embed;
    }
    public EmbedBuilder WithPermissionsRemoved(EmbedBuilder embed, List<FileAttachment> attachments)
    {
        if (PermissionsRemoved.Count < 1) return embed;
        var content = string.Join("\n", PermissionsRemoved.Select(e => e.ToString()));
        if (content.Length >= 1024)
        {
            embed.AddField(
                $"Permissions Removed ({PermissionsRemoved.Count})",
                "-# Attached as `permissions-removed.txt`",
                true);
            attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(content)), "permissions-removed.txt"));
        }
        else if (content.Length > 0)
        {
            embed.AddField(
                $"Permissions Removed ({PermissionsRemoved.Count})",
                content,
                true);
        }
        return embed;
    }
    public EmbedBuilder WithPermissionsUpdated(EmbedBuilder embed, List<FileAttachment> attachments)
    {
        if (!AnyPermissions) return embed;

        var added = string.Join("\n", PermissionsAdded.Select(e => e.ToString()));
        var removed = string.Join("\n", PermissionsRemoved.Select(e => e.ToString()));
        var diffAdd = string.Join("\n", PermissionsAdded.Select(e => "+ " + e.ToString()));
        var diffRem = string.Join("\n", PermissionsRemoved.Select(e => "- " + e.ToString()));
        var content = new List<string>();
        if (!string.IsNullOrEmpty(diffAdd)) content.Add(diffAdd);
        if (!string.IsNullOrEmpty(diffRem)) content.Add(diffRem);
        var diff = string.Join("\n", content);

        if (!FieldNeedAttachment(diff, "diff", true))
        {
            embed.AddField("User Permissions", "```diff\n" + diff + "\n```");
        }
        else if (added.Length < 1024 && removed.Length < 1024)
        {
            if (!string.IsNullOrEmpty(added))
                embed.AddField("Added", added);
            if (!string.IsNullOrEmpty(removed))
                embed.AddField("Removed", removed);
        }
        else
        {
            embed.AddField(
                "User Permissions",
                "> Too many permissions were updated! They've been attached as `permissions.diff`");
            attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(diff)), "permissions.diff"));
        }

        return embed;
    }

    public EmbedBuilder[] CreateEmbed(List<FileAttachment> attachments)
    {
        var result = new List<EmbedBuilder>(2);
        if (PermissionsAdded.Count > 0 || PermissionsRemoved.Count > 0)
        {
            var embed = new EmbedBuilder()
                .WithTitle("User Permissions Updated")
                .WithDescription(string.Join("\n",
                    $"<@{Snapshot.UserId}>",
                    "```",
                    $"Display Name: {Snapshot.Nickname ?? Snapshot.Username}",
                    $"Username: {Snapshot.Username}",
                    $"Id: {Snapshot.UserId}",
                    "```"))
                .WithColor(Color.Orange)
                .WithCurrentTimestamp();
            WithPermissionsUpdated(embed, attachments);
            result.Add(embed);
        }

        if (RolesAdded.Count > 0 && RolesRemoved.Count > 0)
        {
            var embed = new EmbedBuilder()
                .WithTitle("User Roles Updated")
                .WithDescription(string.Join("\n",
                    $"<@{Snapshot.UserId}>",
                    "```",
                    $"Display Name: {Snapshot.Nickname ?? Snapshot.Username}",
                    $"Username: {Snapshot.Username}",
                    $"Id: {Snapshot.UserId}",
                    "```"))
                .WithColor(Color.Orange)
                .WithCurrentTimestamp();
            WithRolesAdded(embed, attachments);
            WithRolesRemoved(embed, attachments);
            result.Add(embed);
        }

        return [.. result];
    }
    private static bool FieldNeedAttachment(string content, string? codeAsType = null, bool code = false)
    {
        if (string.IsNullOrEmpty(codeAsType) && !code)
            return content.Length >= 1024;
        var sb = new StringBuilder("```");
        if (!string.IsNullOrEmpty(codeAsType))
            sb.Append(codeAsType);
        sb.AppendLine();
        sb.Append(content);
        sb.AppendLine();
        sb.Append("```");
        return sb.Length >= 1024;
    }
}