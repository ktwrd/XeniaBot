using Discord;
using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Common.Services;

public class DiscordCacheService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaDbContext _db;
    public DiscordCacheService(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var _);
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

        await InsertOrUpdate(db, guildModel);
    }
    #endregion

    #region Guilld Member
    public async Task UpdateGuildMember(
        IGuild guild, IUser user,
        UpdateGuildMemberSource source = UpdateGuildMemberSource.Unknown)
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
            member = await guild.GetUserAsync(userId);
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
            if (member is IGuildUser guildUser)
            {
                model.JoinedAt = guildUser.JoinedAt.HasValue
                    ? guildUser.JoinedAt.Value.UtcDateTime
                    : null;
            }
            model.FirstJoinedAt ??= model.JoinedAt;
        }
        await InsertOrUpdate(db, model);
    }
    #endregion

    private async Task InsertOrUpdate(
        XeniaDbContext db,
        GuildMemberCacheModel model)
    {
        var now = DateTime.UtcNow;
        if (await db.GuildMemberCache.AnyAsync(e => e.GuildId == model.GuildId && e.UserId == model.UserId))
        {
            await db.GuildMemberCache.Where(e => e.GuildId == model.GuildId && e.UserId == model.UserId)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.IsMember, model.IsMember)
                .SetProperty(p => p.JoinedAt, model.JoinedAt)
                .SetProperty(p => p.FirstJoinedAt, model.FirstJoinedAt)
                .SetProperty(p => p.UpdatedAt, now));
        }
        else
        {
            await db.GuildMemberCache.AddAsync(model);
        }
    }
    private async Task InsertOrUpdate(
        XeniaDbContext db,
        GuildCacheModel model)
    {
        var now = DateTime.UtcNow;
        if (await db.GuildCache.AnyAsync(e => e.Id == model.Id))
        {
            await db.GuildCache.Where(e => e.Id == model.Id)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.Name, model.Name)
                .SetProperty(p => p.OwnerUserId, model.OwnerUserId)
                .SetProperty(p => p.CreatedAt, model.CreatedAt)
                .SetProperty(p => p.JoinedAt, model.JoinedAt)
                .SetProperty(p => p.RecordUpdatedAt, now));
        }
        else
        {
            await db.GuildCache.AddAsync(model);
        }
    }

    public enum UpdateGuildMemberSource
    {
        Unknown,
        UserLeft,
        UserJoined
    }
}
