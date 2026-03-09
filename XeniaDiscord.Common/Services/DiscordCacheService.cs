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
        var guildUsers = await guild.GetUsersAsync();
        await Task.WhenAll(guildUsers.Select(PerformGuildMember));

        async Task PerformGuildMember(IGuildUser member)
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
    public async Task UpdateGuildMember(
        XeniaDbContext db,
        IGuild guild, ulong userId)
    {
        IGuildUser? member = null;
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
    public async Task UpdateGuildMember(XeniaDbContext db, IGuild guild, ulong userId, IGuildUser? member)
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
            model.JoinedAt = member.JoinedAt.HasValue
                ? member.JoinedAt.Value.UtcDateTime
                : null;
            model.FirstJoinedAt ??= model.JoinedAt;
        }
        await InsertOrUpdate(db, model);
    }

    private async Task InsertOrUpdate(
        XeniaDbContext db, 
        GuildMemberCacheModel model)
    {
        var now = DateTime.UtcNow;
        if (await db.GuildMemberCache.AnyAsync(e => e.GuildId == model.GuildId && e.UserId == model.UserId))
        {
            await db.GuildMemberCache.AddAsync(model);
        }
        else
        {
            await db.GuildMemberCache.Where(e => e.GuildId == model.GuildId && e.UserId == model.UserId)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.IsMember, model.IsMember)
                .SetProperty(p => p.JoinedAt, model.JoinedAt)
                .SetProperty(p => p.FirstJoinedAt, model.FirstJoinedAt)
                .SetProperty(p => p.UpdatedAt, now));
        }
    }
}
