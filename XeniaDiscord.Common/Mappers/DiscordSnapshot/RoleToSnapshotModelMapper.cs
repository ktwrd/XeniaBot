using Discord;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Common.Mappers.DiscordSnapshot;

public class RoleToSnapshotModelMapper
    : IMapper<IRole, GuildRoleSnapshotModel>
{
    public static void RegisterService(IServiceCollection services)
    {
        services.AddSingleton<RoleToSnapshotModelMapper>()
                .AddSingleton<IMapper<IRole, GuildRoleSnapshotModel>, RoleToSnapshotModelMapper>();
    }
    public GuildRoleSnapshotModel Map(IRole source) => MapInternal(source);
    private static GuildRoleSnapshotModel MapInternal(IRole? role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new GuildRoleSnapshotModel
        {
            RecordCreatedAt = DateTime.UtcNow,

            GuildId = role.Guild.Id.ToString(),
            RoleId = role.Id.ToString(),
            Name = role.Name,
            CreatedAt = role.CreatedAt.UtcDateTime,
            PermissionsValue = role.Permissions.RawValue.ToString(),
            Position = role.Position,
            IconHash = string.IsNullOrEmpty(role.Icon) ? null : role.Icon,
            IsManaged = role.IsManaged,
            IsMentionable = role.IsMentionable,
            IsHoisted = role.IsHoisted,
        };
    }
}
