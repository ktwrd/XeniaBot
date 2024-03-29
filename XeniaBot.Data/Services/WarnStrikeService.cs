using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared;

namespace XeniaBot.Data.Services;

[XeniaController]
public class WarnStrikeService : BaseService
{
    private readonly GuildWarnItemRepository _guildWarnRepo;
    private readonly GuildConfigWarnStrikeRepository _configWarnStrikeRepo;

    public WarnStrikeService(IServiceProvider services)
        : base(services)
    {
        _guildWarnRepo = services.GetRequiredService<GuildWarnItemRepository>();
        _configWarnStrikeRepo = services.GetRequiredService<GuildConfigWarnStrikeRepository>();
    }

    /// <summary>
    /// <para>Fetch <see cref="GuildConfigWarnStrikeModel"/></para>
    ///
    /// <para>When it doesn't exist, just create a new one without inserting into the database.</para>
    /// </summary>
    /// <param name="guildId"><see cref="GuildConfigWarnStrikeModel.GuildId"/></param>
    public async Task<GuildConfigWarnStrikeModel> GetStrikeConfig(ulong guildId)
    {
        var model = await _configWarnStrikeRepo.GetByGuild(guildId);
        model ??= new GuildConfigWarnStrikeModel()
        {
            GuildId = guildId
        };
        return model;
    }

    /// <summary>
    /// <para>When Warn Strike system is enabled on the Guild provided, only return warns that are older than <see cref="GuildConfigWarnStrikeModel.StrikeWindow"/> seconds ago.</para>
    ///
    /// <para>Otherwise, just return the results of <see cref="GetAllWarnsForUser"/></para>
    /// </summary>
    /// <param name="guildId"><see cref="GuildWarnItemModel.GuildId"/></param>
    /// <param name="userId"><see cref="GuildWarnItemModel.TargetUserId"/></param>
    /// <returns>List of documents that match the criteria</returns>
    public async Task<List<GuildWarnItemModel>?> GetActiveWarnsForUser(ulong guildId, ulong userId)
    {
        var config = await GetStrikeConfig(guildId);
        if (!config.EnableStrikeSystem)
        {
            return await GetAllWarnsForUser(guildId, userId);
        }

        var minimumCreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - (config.StrikeWindow * 1000);
        var data = await _guildWarnRepo.GetLatestGuildMemberItems(guildId, userId, Convert.ToInt64(minimumCreatedAt));
        return data?.ToList();
    }

    /// <summary>
    /// <para>Has the User reached the Warn Limit in the Guild Provided?</para>
    ///
    /// <para>When the Strike System is disabled, it will return (false, null)</para>
    /// </summary>
    /// <returns>
    /// <para><b>Item1:</b> Has the user reached the Warn Limit?</para>
    /// <para><b>Item2:</b> Output of <see cref="GetActiveWarnsForUser"/></para>
    /// </returns>
    public async Task<(bool, List<GuildWarnItemModel>?)> UserReachedWarnLimit(ulong guildId, ulong userId)
    {
        var config = await GetStrikeConfig(guildId);
        if (!config.EnableStrikeSystem)
        {
            return (false, null);
        }

        var data = await GetActiveWarnsForUser(guildId, userId);
        return (data?.Count >= config.MaxStrike, data);
    }

    /// <inheritdoc cref="GuildConfigWarnStrikeRepository.GetLatestGuildMemberItems(ulong, ulong, long)"/>
    public async Task<List<GuildWarnItemModel>?> GetAllWarnsForUser(ulong guildId, ulong userId)
    {
        var data = await _guildWarnRepo.GetLatestGuildMemberItems(guildId, userId);
        return data?.ToList();
    }
}