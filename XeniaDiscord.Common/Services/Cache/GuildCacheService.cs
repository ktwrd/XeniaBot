using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Common.Services;

public class GuildCacheService
{
    private readonly XeniaDbContext _db;
    private readonly DiscordSocketClient _client;
    private readonly GuildCacheRepository _repo;
    private readonly IMapper<IGuild, GuildCacheModel> _mapper;

    public GuildCacheService(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _mapper = services.GetRequiredService<IMapper<IGuild, GuildCacheModel>>();

        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _repo = (scope?.ServiceProvider ?? services).GetRequiredService<GuildCacheRepository>();
    }

    public async Task<string?> GetIconUrl(ulong id, bool saveChages = true)
    {
        var idStr = id.ToString();
        var existingUrl = await _db.GuildCache.AsNoTracking()
            .Where(e => e.Id == idStr)
            .Select(e => e.IconUrl)
            .Take(1)
            .ToArrayAsync();
        if (existingUrl.Length == 1)
            return existingUrl[0];

        IGuild? guild = null;
        try
        {
            guild = _client.GetGuild(id);
        }
        catch { }
        if (guild == null) return null;

        var mapped = _mapper.Map(guild);
        await _repo.InsertOrUpdate(_db, mapped);
        if (saveChages)
        {
            await _db.SaveChangesAsync();
        }
        return mapped.IconUrl;
    }
}
