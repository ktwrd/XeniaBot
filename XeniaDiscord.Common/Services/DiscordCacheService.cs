using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Cache;
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

    public DiscordCacheService(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var _);

        _client = services.GetRequiredService<DiscordSocketClient>();
        _userCacheRepository = services.GetRequiredService<UserCacheRepository>();
        _guildCacheRepository = services.GetRequiredService<GuildCacheRepository>();
        _guildMemberCacheRepository = services.GetRequiredService<GuildMemberCacheRepository>();
        
        _userMapper = services.GetRequiredService<IMapper<IUser, UserCacheModel>>();
        _memberMergerMapper = services.GetRequiredService<IMapperMerger<IUser, GuildMemberCacheModel>>();
    }

    #region Guild
    public async Task UpdateGuild(IGuild guild)
    {
        using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await UpdateGuild(db, guild);
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
        IGuild guild)
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
        }

        var guildIdStr = guild.Id.ToString();
        var guildModel = await db.GuildCache.Where(e => e.Id == guildIdStr)
            .FirstOrDefaultAsync()
            ?? new()
            {
                Id = guildIdStr
            };

        guildModel.Name = guild.Name;
        guildModel.OwnerUserId = guild.OwnerId.ToString();
        guildModel.CreatedAt = guild.CreatedAt.UtcDateTime;
        guildModel.JoinedAt ??= DateTime.UtcNow;
        guildModel.IconUrl = guild.IconUrl;
        guildModel.BannerUrl = guild.BannerUrl;
        guildModel.SplashUrl = guild.SplashUrl;
        guildModel.DiscoverySplashUrl = guild.DiscoverySplashUrl;

        await _guildCacheRepository.InsertOrUpdate(db, guildModel);
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
