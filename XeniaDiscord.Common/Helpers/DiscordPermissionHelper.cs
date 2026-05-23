using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;

namespace XeniaDiscord.Common.Helpers;

public static class DiscordPermissionHelper
{
    public static GuildPermission CalculateGuild(
        SocketGuildUser member)
        => CalculateGuild(member.Roles, member.GuildPermissions);
    public static GuildPermission CalculateGuild(
        IReadOnlyCollection<IRole> memberRoles,
        GuildPermissions memberPermissions)
    {
        var permissions = (GuildPermission)((ulong)0);
        foreach (var role in memberRoles.OrderBy(e => e.Position))
        {
            foreach (var p in role.Permissions.ToList().Where(e => !permissions.HasFlag(e)))
            {
                permissions |= p;
            }
        }

        foreach (var p in memberPermissions.ToList().Where(e => !permissions.HasFlag(e)))
        {
            permissions |= p;
        }

        return permissions;
    }

    public static CalculateChannelPermissionsResult CalculateChannel(
        SocketGuildUser member,
        IGuildChannel channel)
        => CalculateChannel(
            member.Roles,
            member.GuildPermissions,
            member.Id,
            channel.PermissionOverwrites);

    public static CalculateChannelPermissionsResult CalculateChannel(
        IReadOnlyCollection<IRole> memberRoles,
        GuildPermissions memberPermissions,
        ulong memberId,
        IReadOnlyCollection<Overwrite> permissionOverwrites)
    {
        var allow = (ChannelPermission)(0);
        var deny = (ChannelPermission)(0);
        foreach (var overwrite in memberRoles
                     .OrderBy(static e => e.Position)
                     .Select(r => permissionOverwrites.FirstOrDefault(p => p.TargetId == r.Id && p.TargetType == PermissionTarget.Role)))
        {
            ProcessChannelOverwrite(ref allow, ref deny, overwrite);
        }

        var userOverwrite = permissionOverwrites
            .Where(e => e.TargetId == memberId && e.TargetType == PermissionTarget.User)
            .Take(1)
            .ToArray();
        if (userOverwrite.Length > 0)
        {
            ProcessChannelOverwrite(ref allow, ref deny, userOverwrite[0]);
        }

        return new CalculateChannelPermissionsResult(allow, deny, CalculateGuild(memberRoles, memberPermissions));
    }

    public static void ProcessChannelOverwrite(
        ref ChannelPermission allow,
        ref ChannelPermission deny,
        Overwrite overwrite)
    {
        var allowValue = (ChannelPermission)overwrite.Permissions.AllowValue;
        var denyValue = (ChannelPermission)overwrite.Permissions.DenyValue;
        foreach (var f in Enum.GetValues<ChannelPermission>())
        {
            if (allowValue.HasFlag(f) && !allow.HasFlag(f)) allow |= f;
            if (denyValue.HasFlag(f) && !deny.HasFlag(f)) deny |= f;
        }
    }

    public static bool CanPerform(this CalculateChannelPermissionsResult data,
        ChannelPermission permission)
    {
        if (data.Allow.HasFlag(permission)) return true;
        if (data.Deny.HasFlag(permission)) return true;
        return permission.ToGuildPermission()
            .GetValueOrDefault(
                v => data.GuildPermissions.HasFlag(v),
                defaultValue: false);
    }

    public static Maybe<GuildPermission> ToGuildPermission(this ChannelPermission value)
    {
        return value switch
        {
            ChannelPermission.CreateInstantInvite => GuildPermission.CreateInstantInvite,
            ChannelPermission.ManageChannels => GuildPermission.ManageChannels,
            ChannelPermission.AddReactions => GuildPermission.AddReactions,
            ChannelPermission.ViewChannel => GuildPermission.ViewChannel,
            ChannelPermission.SendMessages => GuildPermission.SendMessages,
            ChannelPermission.SendTTSMessages => GuildPermission.SendTTSMessages,
            ChannelPermission.ManageMessages => GuildPermission.ManageMessages,
            ChannelPermission.EmbedLinks => GuildPermission.EmbedLinks,
            ChannelPermission.AttachFiles => GuildPermission.AttachFiles,
            ChannelPermission.ReadMessageHistory => GuildPermission.ReadMessageHistory,
            ChannelPermission.MentionEveryone => GuildPermission.MentionEveryone,
            ChannelPermission.UseExternalEmojis => GuildPermission.UseExternalEmojis,
            ChannelPermission.Connect => GuildPermission.Connect,
            ChannelPermission.Speak => GuildPermission.Speak,
            ChannelPermission.MuteMembers => GuildPermission.MuteMembers,
            ChannelPermission.DeafenMembers => GuildPermission.DeafenMembers,
            ChannelPermission.MoveMembers => GuildPermission.MoveMembers,
            ChannelPermission.UseVAD => GuildPermission.UseVAD,
            ChannelPermission.PrioritySpeaker => GuildPermission.PrioritySpeaker,
            ChannelPermission.Stream => GuildPermission.Stream,
            ChannelPermission.ManageRoles => GuildPermission.ManageRoles,
            ChannelPermission.ManageWebhooks => GuildPermission.ManageWebhooks,
            ChannelPermission.ManageEmojis => GuildPermission.ManageEmojisAndStickers,
            ChannelPermission.UseApplicationCommands => GuildPermission.UseApplicationCommands,
            ChannelPermission.RequestToSpeak => GuildPermission.RequestToSpeak,
            ChannelPermission.ManageThreads => GuildPermission.ManageThreads,
            ChannelPermission.CreatePublicThreads => GuildPermission.CreatePublicThreads,
            ChannelPermission.CreatePrivateThreads => GuildPermission.CreatePrivateThreads,
            ChannelPermission.UseExternalStickers => GuildPermission.UseExternalStickers,
            ChannelPermission.SendMessagesInThreads => GuildPermission.SendMessagesInThreads,
            ChannelPermission.StartEmbeddedActivities => GuildPermission.StartEmbeddedActivities,
            ChannelPermission.UseSoundboard => GuildPermission.UseSoundboard,
            ChannelPermission.CreateEvents => GuildPermission.CreateEvents,
            ChannelPermission.UseExternalSounds => GuildPermission.UseExternalSounds,
            ChannelPermission.SendVoiceMessages => GuildPermission.SendVoiceMessages,
            ChannelPermission.UseClydeAI => GuildPermission.UseClydeAI,
            ChannelPermission.SetVoiceChannelStatus => GuildPermission.SetVoiceChannelStatus,
            ChannelPermission.SendPolls => GuildPermission.SendPolls,
            ChannelPermission.UseExternalApps => GuildPermission.UseExternalApps,
            ChannelPermission.PinMessages => GuildPermission.PinMessages,
            ChannelPermission.BypassSlowmode => GuildPermission.BypassSlowmode,
            _ => Maybe.None
        };
    }
}

public sealed record CalculateChannelPermissionsResult(
    ChannelPermission Allow,
    ChannelPermission Deny,
    GuildPermission GuildPermissions);