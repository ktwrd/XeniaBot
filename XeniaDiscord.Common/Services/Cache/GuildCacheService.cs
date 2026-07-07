using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Common.Services;

public class GuildCacheService
{
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    private readonly DiscordSocketClient _client;
    private readonly GuildCacheRepository _repo;
    private readonly IMapper<IGuild, GuildCacheModel> _mapper;

    public GuildCacheService(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _mapper = services.GetRequiredService<IMapper<IGuild, GuildCacheModel>>();

        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        _repo = services.GetRequiredService<GuildCacheRepository>();
    }

    public async Task<string?> GetIconUrl(ulong id, bool saveChanges = true)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await GetIconUrl(db, id, saveChanges);
    }
    public async Task<string?> GetIconUrl(XeniaDbContext db, ulong id, bool saveChanges = true)
    {
        var idStr = id.ToString();
        var existingUrl = await db.GuildCache
            .AsNoTracking()
            .Where(e => e.Id == idStr)
            .Select(e => e.IconUrl)
            .Take(1)
            .ToArrayAsync();
        if (existingUrl.Length == 1)
            return existingUrl[0];

        IGuild? guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(id));
        if (guild == null) return null;

        var mapped = _mapper.Map(guild);
        await _repo.InsertOrUpdate(db, mapped);
        if (saveChanges)
        {
            await db.SaveChangesAsync();
        }
        return mapped.IconUrl;
    }
}
