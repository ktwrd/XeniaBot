using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Warn;

namespace XeniaDiscord.Common.Repositories;

public class WarnRepository : IWarnRepository
{
    private readonly ApplicationDbContext _db;
    public WarnRepository(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    private async Task<GuildWarnConfigModel> GetOrCreateConfig(string guildIdStr, string? createdByUserIdStr)
    {
        var model = await _db.GuildWarnConfigs
            .AsNoTracking()
            .Where(e => e.Id == guildIdStr)
            .FirstOrDefaultAsync();
        if (model != null)
            return model;

        model = new GuildWarnConfigModel
        {
            Id = guildIdStr,
            CreatedByUserId = createdByUserIdStr ?? "0"
        };
        await _db.GuildWarnConfigs.AddAsync(model);
        await _db.SaveChangesAsync();
        return model;
    }

    private async Task<GuildWarnConfigModel> UpdateLogChannel(string? channelIdStr, string guildIdStr, string? updatedByUserIdStr)
    {
        var config = await GetOrCreateConfig(guildIdStr, updatedByUserIdStr);
        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var updatedAt = DateTime.UtcNow;
            await ctx.GuildWarnConfigs
                .Where(e => e.Id == config.Id)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.LogChannelId, channelIdStr)
                    .SetProperty(p => p.UpdatedByUserId, updatedByUserIdStr)
                    .SetProperty(p => p.UpdatedAt, updatedAt));
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
            return await GetOrCreateConfig(guildIdStr, updatedByUserIdStr);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<bool> SetLoggingState(string guildIdStr, string? updatedByUserIdStr, bool state)
    {
        await using var ctx = _db.CreateSession();
        await using var transaction = await ctx.Database.BeginTransactionAsync();
        try
        {
            var affected = await ctx.GuildWarnConfigs
                .Where(e => e.Id == guildIdStr)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.EnableLogging, state)
                    .SetProperty(p => p.UpdatedByUserId, updatedByUserIdStr ?? "0")
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
            if (affected > 1)
            {
                throw new InvalidOperationException($"This query was going to affect {affected} records, but the transaction was rolled back. (guildId: {guildIdStr}, updatedBy: {updatedByUserIdStr}, state: {state})");
            }
            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
            return affected == 1;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public Task<GuildWarnConfigModel> GetOrCreateConfig(IGuild guild, IUser? createdByUser)
        => GetOrCreateConfig(guild.Id.ToString(), createdByUser?.Id.ToString());

    /// <inheritdoc />
    public Task<GuildWarnConfigModel> UpdateLogChannel(ITextChannel? channel, IGuild guild, IUser? updatedByUser)
        => UpdateLogChannel(channel?.Id.ToString(), guild.Id.ToString(), updatedByUser?.Id.ToString());

    public Task<bool> EnableLogging(IGuild guild, IUser? updatedByUser)
        => SetLoggingState(guild.Id.ToString(), updatedByUser?.Id.ToString(), true);
    public Task<bool> DisableLogging(IGuild guild, IUser? updatedByUser)
        => SetLoggingState(guild.Id.ToString(), updatedByUser?.Id.ToString(), false);
}
