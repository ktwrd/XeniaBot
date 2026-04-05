using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Data;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Common.Services;

public class DiscordSnapshotService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaDbContext _db;
    private readonly DiscordSocketClient _client;
    private readonly DiscordCacheService _cacheService;
    private readonly IMapper<IRole, GuildRoleSnapshotModel> _roleMapper;
    private readonly IMapper<IGuildUser, GuildMemberSnapshotModel> _guildMemberMapper;

    private readonly ErrorReportService _err;
    public DiscordSnapshotService(IServiceProvider services) : base(services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var _);
        _client = services.GetRequiredService<DiscordSocketClient>();
        _cacheService = services.GetRequiredService<DiscordCacheService>();

        _roleMapper = services.GetRequiredService<IMapper<IRole, GuildRoleSnapshotModel>>();
        _guildMemberMapper = services.GetRequiredService<IMapper<IGuildUser, GuildMemberSnapshotModel>>();

        _err = services.GetRequiredService<ErrorReportService>();

        var programDetails = services.GetRequiredService<ProgramDetails>();
        if (programDetails.Platform == XeniaPlatform.Bot)
        {
            _client.JoinedGuild += OnGuildJoined;
            _client.UserJoined += OnGuildMemberJoined;
            _client.GuildMemberUpdated += OnGuildMemberUpdated;
            _client.RoleCreated += OnGuildRoleCreated;
            _client.RoleUpdated += OnGuildRoleUpdated;
        }
    }

    public event DiscordSnapshotComparisonDelegate<GuildMemberSnapshotModel>? GuildMemberUpdated;
    public event DiscordSnapshotComparisonSourceDelegate<GuildRoleSnapshotModel>? GuildRoleUpdated;
    public event DiscordSnapshotSourceDelegate<GuildRoleSnapshotModel>? GuildRoleDeleted;

    private Task OnGuildJoined(SocketGuild guild)
    {
        new Thread(() =>
        {
            try
            {
                ProcessGuild(guild).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process Guild \"{guild.Name}\" ({guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private Task OnGuildMemberJoined(SocketGuildUser member)
    {
        new Thread(() =>
        {
            try
            {
                ProcessGuildMember(member).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process {member} ({member.Id}) in guild {member.Guild.Name} ({member.Guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private Task OnGuildMemberUpdated(
        Cacheable<SocketGuildUser, ulong> before,
        SocketGuildUser member)
    {
        new Thread(() =>
        {
            try
            {
                ProcessGuildMember(member).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process {member} ({member.Id}) in guild {member.Guild.Name} ({member.Guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private Task OnGuildRoleCreated(SocketRole role)
    {
        new Thread(() =>
        {
            try
            {
                ProcessRole(DiscordSnapshotEventSource.Create, null, role).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process role {role.Name} ({role.Id}) in guild {role.Guild.Name} ({role.Guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private Task OnGuildRoleUpdated(SocketRole? roleBefore, SocketRole role)
    {
        new Thread(() =>
        {
            try
            {
                ProcessRole(DiscordSnapshotEventSource.Edit, roleBefore, role).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to process role {role.Name} ({role.Id}) in guild {role.Guild.Name} ({role.Guild.Id})");
            }
        }).Start();
        return Task.CompletedTask;
    }

    private async Task ProcessGuild(SocketGuild guild)
    {
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await UpdateGuild(db, guild);
            await _cacheService.UpdateGuild(db, guild);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            _log.Error(ex, $"Failed to pull data for Guild \"{guild.Name}\" ({guild.Id})");
        }
    }

    private async Task ProcessGuildMember(SocketGuildUser socketMemberAfter)
    {
        var userIdStr = socketMemberAfter.Id.ToString();
        var guildIdStr = socketMemberAfter.Guild.Id.ToString();
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        GuildMemberSnapshotModel? modelBefore = null;
        try
        {
            modelBefore = await db.GuildMemberSnapshots.AsNoTracking()
                .Include(e => e.Roles)
                .Include(e => e.Permissions)
                .OrderByDescending(e => e.RecordCreatedAt)
                .FirstOrDefaultAsync(e => e.GuildId == guildIdStr && e.UserId == userIdStr);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to find Guild Member Snapshot (userId={userIdStr},guildId={guildIdStr})";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(socketMemberAfter));
        }

        GuildMemberSnapshotModel model;
        try
        {
            model = _guildMemberMapper.Map(socketMemberAfter);
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            var msg = $"Failed to map {socketMemberAfter.GetType()} (userId: {userIdStr}, guildId: {guildIdStr})";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(socketMemberAfter));
            return;
        }
        try
        {
            await db.AddAsync(model);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            var msg = $"Failed to add record into database";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithUser(socketMemberAfter)
                .AddSerializedAttachment("model.json", model));
            return;
        }
        GuildMemberUpdated?.Invoke(modelBefore, model);
    }

    private async Task ProcessRole(
        DiscordSnapshotEventSource source,
        SocketRole? roleBefore,
        SocketRole role)
    {
        var roleIdStr = role.Id.ToString();
        var guildIdStr = role.Guild.Id.ToString();
        GuildRoleSnapshotModel? modelBefore = null;
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            modelBefore = await db.GuildRoleSnapshots.AsNoTracking()
                .OrderByDescending(e => e.RecordCreatedAt)
                .FirstOrDefaultAsync(e => e.GuildId == guildIdStr && e.RoleId == roleIdStr);
        }
        catch (Exception ex)
        {
            var msg = $"Failed to find Guild Role Snapshot (roleId={roleIdStr},guildId={guildIdStr})";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithRole(role));
        }
        if (modelBefore == null && roleBefore != null)
        {
            try
            {
                modelBefore = _roleMapper.Map(roleBefore);
                var c = modelBefore.RecordCreatedAt - TimeSpan.FromSeconds(1);
                if (c < modelBefore.CreatedAt) c = modelBefore.CreatedAt;
                modelBefore.RecordCreatedAt = c;
                await db.AddAsync(modelBefore);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to map \"before\" state of Role \"{role.Name}\" ({role.Id}) in Guild \"{role.Guild.Name}\" ({role.Guild.Id})";
                await _err.Submit(new ErrorReportBuilder()
                    .WithException(ex)
                    .WithNotes(msg)
                    .WithRole(roleBefore));
            }
        }

        GuildRoleSnapshotModel model;
        try
        {
            model = _roleMapper.Map(role);
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();

            var msg = $"Failed to map Role \"{role.Name}\" ({role.Id}) in Guild \"{role.Guild.Name}\" ({role.Guild.Id})";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithRole(role));
            return;
        }
        try
        {
            await db.AddAsync(model);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();

            var msg = $"Failed to add record into database";
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithRole(role)
                .AddSerializedAttachment("model.json", model));
            return;
        }
        GuildRoleUpdated?.Invoke(source, modelBefore, model);
    }

    public async Task UpdateGuild(
        XeniaDbContext db,
        IGuild guild)
    {
        var members = new List<GuildMemberSnapshotModel>();
        var roles = new List<GuildRoleSnapshotModel>();
        foreach (var member in await guild.GetUsersAsync())
        {
            try
            {
                var mapped = _guildMemberMapper.Map(member);
                members.Add(mapped);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to map member \"{member.GlobalName}\" ({member.Username}, {member.Id}) for Guild \"{guild.Name}\" ({guild.Id})");
            }
        }
        foreach (var role in guild.Roles)
        {
            try
            {
                var mapped = _roleMapper.Map(role);
                roles.Add(mapped);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to map role \"{role.Name}\" ({role.Id}) for Guild \"{guild.Name}\" ({guild.Id})");
            }
        }

        try
        {
            await db.AddRangeAsync(members);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to save members for Guild \"{guild.Name}\" ({guild.Id})");
        }
        try
        {
            await db.AddRangeAsync(roles);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to save roles for Guild \"{guild.Name}\" ({guild.Id})");
        }
    }
}

public delegate Task DiscordSnapshotComparisonDelegate<in TModel>(TModel? before, TModel model);
public delegate Task DiscordSnapshotComparisonSourceDelegate<in TModel>(DiscordSnapshotEventSource source, TModel? before, TModel model);
public delegate Task DiscordSnapshotSourceDelegate<in TModel>(DiscordSnapshotEventSource source, ulong id, TModel? snapshot);

public enum DiscordSnapshotEventSource
{
    Create,
    Edit,
    Delete
}