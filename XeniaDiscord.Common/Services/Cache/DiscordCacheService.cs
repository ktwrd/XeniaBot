using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Common.Services;

[UsedImplicitly]
public class DiscordCacheService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;

    private readonly DiscordSocketClient _client;
    private readonly UserCacheRepository _userCacheRepository;
    private readonly GuildCacheRepository _guildCacheRepository;
    private readonly GuildMemberCacheRepository _guildMemberCacheRepository;

    private readonly IMapper<IUser, UserCacheModel> _userMapper;
    private readonly IMapperMerger<IUser, GuildMemberCacheModel> _memberMergerMapper;
    private readonly IMapperMerger<IGuild, GuildCacheModel> _guildMergerMapper;
    public DiscordCacheService(IServiceProvider services)
    {
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();

        _client = services.GetRequiredService<DiscordSocketClient>();
        _userCacheRepository = services.GetRequiredService<UserCacheRepository>();
        _guildCacheRepository = services.GetRequiredService<GuildCacheRepository>();
        _guildMemberCacheRepository = services.GetRequiredService<GuildMemberCacheRepository>();
        
        _userMapper = services.GetRequiredService<IMapper<IUser, UserCacheModel>>();
        _memberMergerMapper = services.GetRequiredService<IMapperMerger<IUser, GuildMemberCacheModel>>();
        _guildMergerMapper = services.GetRequiredService<IMapperMerger<IGuild, GuildCacheModel>>();
    }

    #region Guild
    public async Task UpdateGuild(IGuild guild)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
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
        IGuild guild,
        DateTime? now = null,
        bool includeMembers = true)
    {
        var nowValue = now ?? DateTime.UtcNow;

        var guildIdStr = guild.Id.ToString();
        var guildModel = await db.GuildCache.Where(e => e.Id == guildIdStr)
            .FirstOrDefaultAsync()
            ?? new GuildCacheModel(guild.Id)
            {
                RecordCreatedAt = nowValue,
                RecordUpdatedAt = nowValue
            };

        guildModel = _guildMergerMapper.Map(guildModel, guild);
        guildModel.RecordUpdatedAt = nowValue;

        await _guildCacheRepository.InsertOrUpdate(db, guildModel);

        if (!includeMembers) return;

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
    }
    #endregion

    #region Guilld Member
    public Task UpdateGuildMember(IGuildUser member) => UpdateGuildMember(member.Guild, member);
    public async Task UpdateGuildMember(IGuild guild, IUser user)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
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
#pragma warning disable S6966
        IUser? member = null;
        try
        {
            member = await ExceptionHelper.RetryOnTimedOut(async () => await guild.GetUserAsync(userId));
            // ReSharper disable once AsyncMethodWithoutAwait
            member ??= await ExceptionHelper.RetryOnTimedOut(async () => _client.GetUser(userId));
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Failed to get Member {userId} in Guild \"{guild.Name}\" ({guild.Id})");
        }
#pragma warning restore S6966
        await UpdateGuildMember(db, guild, userId, member);
    }
    
    public async Task UpdateGuildMember(XeniaDbContext db, IGuild guild, ulong userId, IUser? member,
        DateTime? now = null)
    {
        var nowValue = now ?? DateTime.UtcNow;
        var guildIdStr = guild.Id.ToString();
        var userIdStr = userId.ToString();
        var model = await db.GuildMemberCache
            .FirstOrDefaultAsync(e => e.UserId == userIdStr && e.GuildId == guildIdStr)
            ?? new GuildMemberCacheModel
            {
                GuildId = guildIdStr,
                UserId = userIdStr,
                RecordCreatedAt = nowValue,
                RecordUpdatedAt = nowValue
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
        model.RecordUpdatedAt = nowValue;

        await _guildMemberCacheRepository.InsertOrUpdate(db, model);
        if (member != null)
        {
            await _userCacheRepository.InsertOrUpdate(db, _userMapper.Map(member));
        }
    }
    #endregion

    public async Task UpdateUser(IUser user)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
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
