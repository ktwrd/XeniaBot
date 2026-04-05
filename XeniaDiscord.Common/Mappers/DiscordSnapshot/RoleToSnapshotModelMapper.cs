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

        var result = new GuildRoleSnapshotModel
        {
            RecordCreatedAt = DateTime.UtcNow,

            GuildId = role.Guild.Id.ToString(),
            RoleId = role.Id.ToString(),
            Name = role.Name,
            CreatedAt = role.CreatedAt.UtcDateTime,
            Position = role.Position,
            IconHash = string.IsNullOrEmpty(role.Icon) ? null : role.Icon,
            IsManaged = role.IsManaged,
            IsMentionable = role.IsMentionable,
            IsHoisted = role.IsHoisted,
        };

        foreach (var value in role.Permissions.ToList())
        {
            result.Permissions.Add(new GuildRolePermissionSnapshotModel
            {
                GuildRoleSnapshotId = result.Id,
                RecordCreatedAt = result.RecordCreatedAt,
                GuildId = result.GuildId,
                RoleId = result.RoleId,
                Value = ((ulong)value).ToString(),
            });
        }

        return result;
    }
}
