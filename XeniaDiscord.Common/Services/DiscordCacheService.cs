using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Data;
using XeniaBot.Shared;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Models.Snapshot;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Common.Services;

public class DiscordCacheService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaDbContext _db;

    private readonly DiscordSocketClient _client;
    private readonly UserCacheRepository _userCacheRepository;
    private readonly GuildCacheRepository _guildCacheRepository;
    private readonly GuildMemberCacheRepository _guildMemberCacheRepository;

    private readonly IMapper<IUser, UserCacheModel> _userMapper;
    private readonly IMapperMerger<IUser, GuildMemberCacheModel> _memberMergerMapper;
    private readonly IMapperMerger<IGuild, GuildCacheModel> _guildMergerMapper;
    private readonly IMapper<IGuildUser, GuildMemberSnapshotModel> _guildMemberSnapshotMapper;
    private readonly IMapper<IRole, GuildRoleSnapshotModel> _guildRoleSnapshotMapper;
    public DiscordCacheService(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var _);

        _client = services.GetRequiredService<DiscordSocketClient>();
        _userCacheRepository = services.GetRequiredService<UserCacheRepository>();
        _guildCacheRepository = services.GetRequiredService<GuildCacheRepository>();
        _guildMemberCacheRepository = services.GetRequiredService<GuildMemberCacheRepository>();
        
        _userMapper = services.GetRequiredService<IMapper<IUser, UserCacheModel>>();
        _memberMergerMapper = services.GetRequiredService<IMapperMerger<IUser, GuildMemberCacheModel>>();
        _guildMergerMapper = services.GetRequiredService<IMapperMerger<IGuild, GuildCacheModel>>();
        _guildMemberSnapshotMapper = services.GetRequiredService<IMapper<IGuildUser, GuildMemberSnapshotModel>>();
        _guildRoleSnapshotMapper = services.GetRequiredService<IMapper<IRole, GuildRoleSnapshotModel>>();
    }

    #region Guild
    public async Task UpdateGuild(IGuild guild, bool includeSnapsnots = false)
    {
        using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await UpdateGuild(db, guild, includeSnapsnots);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
        }
    }

    public async Task UpdateGuild(
        XeniaDbContext db,
        IGuild guild,
        bool includeSnapsnots)
    {
        foreach (var member in await guild.GetUsersAsync())
        {
            try
            {
                await UpdateGuildMember(db, guild, member.Id, member);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to update member \"{member.GlobalName}\" ({member.Username}, {member.Id}) in guild \"{guild.Name}\" ({guild.Id})");
            }

            if (includeSnapsnots)
            {
                try
                {
                    var mapped = _guildMemberSnapshotMapper.Map(member);
                    await _db.AddAsync(mapped);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, $"Failed to create snapshot of member \"{member.GlobalName}\" ({member.Username}, {member.Id}) in Guild \"{guild.Name}\" ({guild.Id})");
                }
            }
        }

        var guildIdStr = guild.Id.ToString();
        var guildModel = await db.GuildCache.Where(e => e.Id == guildIdStr)
            .FirstOrDefaultAsync()
            ?? new(guild.Id);

        guildModel = _guildMergerMapper.Map(guildModel, guild);

        await _guildCacheRepository.InsertOrUpdate(db, guildModel);

        if (includeSnapsnots)
        {
            foreach (var role in guild.Roles)
            {
                try
                {
                    var mapped = _guildRoleSnapshotMapper.Map(role);
                    await db.AddAsync(mapped);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, $"Failed to create snapshot of role \"{role.Name}\" ({role.Id}) in Guild \"{guild.Name}\" ({guild.Id})");
                }
            }
        }
    }
    #endregion

    #region Guilld Member
    public Task UpdateGuildMember(IGuildUser member) => UpdateGuildMember(member.Guild, member);
    public async Task UpdateGuildMember(IGuild guild, IUser user)
    {
        using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await UpdateGuildMember(db, guild, user.Id, user);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
        }
    }
    
    public async Task UpdateGuildMember(
        XeniaDbContext db,
        IGuild guild, ulong userId)
    {
        IUser? member = null;
        try
        {
            member = await guild.GetUserAsync(userId)
                ?? await _client.GetUserAsync(userId);
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Failed to get Member {userId} in Guild \"{guild.Name}\" ({guild.Id})");
        }
        await UpdateGuildMember(db, guild, userId, member);
    }
    
    public async Task UpdateGuildMember(XeniaDbContext db, IGuild guild, ulong userId, IUser? member)
    {
        var guildIdStr = guild.Id.ToString();
        var userIdStr = userId.ToString();
        var model = await db.GuildMemberCache
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userIdStr && e.GuildId == guildIdStr)
            ?? new()
            {
                GuildId = guildIdStr,
                UserId = userIdStr
            };

        if (member == null)
        {
            model.IsMember = false;
        }
        else
        {
            model = _memberMergerMapper.Map(model, member);
        }

        // JUST TO BE SURE!!!
        model.UserId = userIdStr;
        model.GuildId = guildIdStr;

        await _guildMemberCacheRepository.InsertOrUpdate(db, model);
        if (member != null)
        {
            await _userCacheRepository.InsertOrUpdate(db, _userMapper.Map(member));
        }
    }
    #endregion

    public async Task UpdateUser(IUser user)
    {
        using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var mapped = _userMapper.Map(user);
            await _userCacheRepository.InsertOrUpdate(db, mapped);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
        }
    }

    public enum UpdateGuildMemberSource
    {
        Unknown,
        UserLeft,
        UserJoined
    }

    public enum UpdateUserSource
    {
        Unknown,
        UserLeft,
        UserJoined,
        UserUpdated
    }
}
