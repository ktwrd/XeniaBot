using Discord;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Common.Mappers.DiscordSnapshot;

public class GuildUserToSnapshotModelMapper
    : IMapper<IGuildUser, GuildMemberSnapshotModel>
{
    public static void RegisterService(IServiceCollection services)
    {
        services.AddSingleton<GuildUserToSnapshotModelMapper>()
                .AddSingleton<IMapper<IGuildUser, GuildMemberSnapshotModel>, GuildUserToSnapshotModelMapper>();
    }
    public GuildMemberSnapshotModel Map(IGuildUser source) => InternalMap(source);
    private static GuildMemberSnapshotModel InternalMap(
        IGuildUser? member)
    {
        var instance = new GuildMemberSnapshotModel();
        ArgumentNullException.ThrowIfNull(member);
        
        instance.UserId = member.Id.ToString();
        instance.GuildId = member.GuildId.ToString();

        instance.Username = NOETrim(member.Username);
        instance.Discriminator = GetDiscriminator(member);
        instance.Nickname = NOETrim(member.Nickname);
        instance.IsSelfDeafened = member.IsSelfDeafened;
        instance.IsSelfMuted = member.IsSelfMuted;
        instance.IsSuppressed = member.IsSuppressed;
        instance.IsDeafened = member.IsDeafened;
        instance.IsMuted = member.IsMuted;
        instance.IsStreaming = member.IsStreaming;
        if (member.VoiceChannel != null)
        {
            instance.VoiceChannelId = member.VoiceChannel.Id.ToString();
        }
        instance.GuildAvatarId = NOETrim(member.GuildAvatarId);
        instance.JoinedAt = member.JoinedAt.HasValue ? member.JoinedAt.Value.UtcDateTime : null;
        instance.TimedOutUntil = member.TimedOutUntil.HasValue ? member.TimedOutUntil.Value.UtcDateTime : null;
        instance.Flags = member.Flags;
        instance.PublicFlags = member.PublicFlags;
        instance.IsPending = member.IsPending;

        var roles = new List<GuildMemberRoleSnapshotModel>();
        var permissions = new List<GuildMemberPermissionSnapshotModel>();
        foreach (var id in member.RoleIds)
        {
            roles.Add(new GuildMemberRoleSnapshotModel
            {
                GuildMemberSnapshotId = instance.RecordId,
                UserId = instance.UserId,
                GuildId = instance.GuildId,
                RoleId = id.ToString()
            });
        }
        foreach (var value in member.GuildPermissions.ToList())
        {
            permissions.Add(new GuildMemberPermissionSnapshotModel
            {
                GuildMemberSnapshotId = instance.RecordId,
                UserId = instance.UserId,
                GuildId = instance.GuildId,
                Value = ((ulong)value).ToString()
            });
        }

        instance.Roles = roles;
        instance.Permissions = permissions;

        return instance;
    }
    private static string NOETrim(string? value) => string.IsNullOrEmpty(value?.Trim()) ? "" : value.Trim();
    internal static string? GetDiscriminator(IUser user)
    {
        if (string.IsNullOrEmpty(user.Discriminator?.Trim()?.Trim('0'))) return null;
        return user.DiscriminatorValue.ToString();
    }
}