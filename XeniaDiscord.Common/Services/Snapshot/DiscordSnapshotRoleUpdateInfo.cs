using Discord;
using System.Text;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Common.Services;

public class DiscordSnapshotRoleUpdateInfo
{
    public required IReadOnlyCollection<GuildPermission> PermissionsAdded { get; init; }
    public required IReadOnlyCollection<GuildPermission> PermissionsRemoved { get; init; }
    public required GuildRoleSnapshotModel? SnapshotBefore { get; init; }
    public required GuildRoleSnapshotModel Snapshot { get; init; }
    public GuildRoleSnapshotSource Source => Snapshot.SnapshotSource;

    public bool Any => AnyPermissions
        || (SnapshotBefore != null && SnapshotBefore?.Flags != Snapshot.Flags)
        || (SnapshotBefore != null && SnapshotBefore?.IsManaged != Snapshot.IsManaged)
        || (SnapshotBefore != null && SnapshotBefore?.IsMentionable != Snapshot.IsMentionable)
        || (SnapshotBefore != null && SnapshotBefore?.IsHoisted != Snapshot.IsHoisted)
        || (SnapshotBefore != null && SnapshotBefore?.Name != Snapshot.Name)
        || Source == GuildRoleSnapshotSource.RoleCreate
        || Source == GuildRoleSnapshotSource.RoleDelete;

    public bool AnyPermissions
        => PermissionsAdded.Count > 0
        || PermissionsRemoved.Count > 0;

    public EmbedBuilder WithInfo(EmbedBuilder embed, List<FileAttachment> attachments)
    {
        var info = new List<string>();
        if (SnapshotBefore?.Name != Snapshot.Name &&
            SnapshotBefore?.Name != null &&
            Snapshot.Name != null)
        {
            var before = SnapshotBefore.Name.Replace("`", "'");
            var after = Snapshot.Name.Replace("`", "'");

            info.Add($"Name was updated from `{before}` to `{after}`");
        }
        if (SnapshotBefore?.Flags != Snapshot.Flags)
        {
            if (SnapshotBefore?.Flags.HasFlag(RoleFlags.InPrompt) == true &&
                !Snapshot.Flags.HasFlag(RoleFlags.InPrompt))
            {
                info.Add("Role can **no longer** be selected via onboarding.");
            }
            else if (SnapshotBefore?.Flags.HasFlag(RoleFlags.InPrompt) != true &&
                Snapshot.Flags.HasFlag(RoleFlags.InPrompt))
            {
                info.Add("Role **can now be selected** via onboarding.");
            }
        }
        if (SnapshotBefore != null)
        {
            if (SnapshotBefore.IsMentionable &&
                !Snapshot.IsMentionable)
            {
                info.Add("Role is **not** mentionable anymore.");
            }
            else if (!SnapshotBefore.IsMentionable &&
                Snapshot.IsMentionable)
            {
                info.Add("Role is now mentionable.");
            }
            if (SnapshotBefore.IsHoisted &&
                !Snapshot.IsHoisted)
            {
                info.Add("Role is **not** pinned anymore.");
            }
            else if (!SnapshotBefore.IsHoisted &&
                Snapshot.IsHoisted)
            {
                info.Add("Role is now pinned.");
            }
        }
        if (info.Count < 1) return embed;
        embed.AddField("Details", string.Join("\n\n", info));
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

        if (!EmbedHelper.FieldRequiresAttachment(diff, "diff"))
        {
            embed.AddField("Permissions", "```diff\n" + diff + "\n```");
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
                "Permissions",
                "> Too many permissions were updated! They've been attached as `permissions.diff`");
            attachments.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(diff)), "permissions.diff"));
        }

        return embed;
    }
}
