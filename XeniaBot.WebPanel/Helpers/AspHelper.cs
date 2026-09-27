using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Repositories;
using XeniaBot.MongoData.Services;
using XeniaBot.DiscordCache.Helpers;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Models;
using NLog;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Repositories;
using RolePreserveGuildRepository = XeniaDiscord.Data.Repositories.RolePreserveGuildRepository;
using RolePreserveGuildModel = XeniaDiscord.Data.Models.RolePreserve.RolePreserveGuildModel;
using ServerLogRepository = XeniaDiscord.Data.Repositories.ServerLogRepository;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.WebPanel.Helpers;

public static class AspHelper
{
    public static ulong? GetUserId(HttpContext context)
    {
        if (!(context.User?.Identity?.IsAuthenticated ?? false)) return null;
        var claim = context.User.Claims.FirstOrDefault(e =>
            e.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (ulong.TryParse(claim?.Value, out var value)) return value;
        return null;
    }
    
    public static bool IsCurrentUserAdmin(HttpContext context)
    {
        var userId = GetUserId(context) ?? 0;
        return context.RequestServices.GetRequiredService<ConfigData>().UserWhitelist.Contains(userId);
    }
    
    public static readonly HashSet<string> ValidMessageTypes
        = [
        "primary",
        "secondary",
        "success",
        "danger",
        "warning",
        "info"
        ];

    /*public static async Task FillServerModel(
        ulong serverId,
        IBanSyncBaseRecordsComponent data,
        int cursor,
        ulong? targetUserId,
        HttpContext context)
    {
        await FillServerModel(serverId, (IBanSyncBaseRecords)data, targetUserId, context);
        data.FilterRecordsByUserId = targetUserId;
        var banSyncRecordConfig = Program.Core.GetRequiredService<BanSyncInfoRepository>();
        data.Items = await banSyncRecordConfig.GetInfoAllInGuildPaginate(
            serverId,
            cursor,
            BanSyncMutualRecordsListComponentViewModel.PageSize, 
            targetUserId,
            allowGhost: IsCurrentUserAdmin(context));
    }*/
    public static async Task<T> FillServerModel<T>(
        IServiceProvider services,
        ulong serverId, T data) where T : IBaseServerModel
    {
        var discord = services.GetRequiredService<DiscordShardedClient>();
        var guild = ExceptionHelper.RetryOnTimedOut(() => discord.GetGuild(serverId));
        data.Guild = guild;

        var counterController = services.GetRequiredService<CounterConfigRepository>();
        data.CounterConfig = await counterController.Get(guild)
            ?? new CounterGuildModel()
            {
                GuildId = serverId
            };

        var bansyncGuildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        data.BanSyncConfig = await bansyncGuildRepo.GetAsync(guild.Id)
            ?? new XeniaDiscord.Data.Models.BanSync.BanSyncGuildModel()
            {
                GuildId = guild.Id.ToString()
            };

        var banSyncStateHistory = services.GetRequiredService<BanSyncGuildSnapshotRepository>();
        data.BanSyncStateHistory = await banSyncStateHistory.GetMany(guild.Id);

        var xpConfig = services.GetRequiredService<LevelSystemConfigRepository>();
        data.XpConfig = await xpConfig.Get(guild.Id)
            ?? new LevelSystemConfigModel()
            {
                GuildId = guild.Id
            };

        var logConfig = services.GetRequiredService<ServerLogRepository>();
        data.LogConfig = await logConfig.GetGuild(
            guild.Id,
            new()
            {
                IncludeChannels = true,
                IncludeGuildCache = true
            }) ?? new()
            {
                GuildId = guild.Id.ToString()
            };

        var membersWhoCanAccess = new List<SocketGuildUser>();
        var wh = services.GetRequiredService<XeniaWebHelper>();
        foreach (var item in guild.Users)
        {
            if (wh.CanAccessGuild(guild.Id, item.Id) && !item.IsBot)
                membersWhoCanAccess.Add(item);
        }
        data.UsersWhoCanAccess = membersWhoCanAccess;

        var greeterConfig = services.GetRequiredService<GuildGreeterConfigRepository>();
        data.GreeterConfig = await greeterConfig.GetLatest(guild.Id)
            ?? new GuildGreeterConfigModel()
            {
                GuildId = guild.Id
            };

        var greeterGoodbyeConfig = services.GetRequiredService<GuildGreetByeConfigRepository>();
        data.GreeterGoodbyeConfig = await greeterGoodbyeConfig.GetLatest(guild.Id)
            ?? new GuildByeGreeterConfigModel()
            {
                GuildId = guild.Id
            };

        var warnConfig = services.GetRequiredService<GuildWarnItemRepository>();
        data.WarnItems = await warnConfig.GetLatestGuildItems(guild.Id)
            ?? new List<GuildWarnItemModel>();

        var rolePreserveConfig = services.GetRequiredService<RolePreserveGuildRepository>();
        data.RolePreserve = await rolePreserveConfig.GetAsync(guild.Id)
            ?? new RolePreserveGuildModel()
            {
                GuildId = guild.Id.ToString()
            };

        var banSyncRecordService = services.GetRequiredService<BanSyncRecordRepository>();
        data.BanSyncRecordCount = await banSyncRecordService.CountForGuild(guild.Id);

        var warnStrikeService = services.GetRequiredService<WarnStrikeService>();
        data.WarnStrikeConfig = await warnStrikeService.GetStrikeConfig(guild.Id);

        var confessionRepo = services.GetRequiredService<ConfessionConfigRepository>();
        data.ConfessionConfig = await confessionRepo.GetGuild(guild.Id)
            ?? new ConfessionGuildModel()
            {
                GuildId = guild.Id
            };
        
        return data;
    }

    public static DateTime DateTimeFromTimestamp(long timestamp, bool seconds = false)
    {
        // Unix timestamp is seconds past epoch
        var dateTime = DateTime.UnixEpoch;
        if (timestamp < 1)
            return dateTime;
        if (seconds)
        {
            dateTime = dateTime.AddSeconds(timestamp).ToLocalTime();
        }
        else
        {
            dateTime = dateTime.AddMilliseconds(timestamp).ToLocalTime();
        }
        return dateTime;
    }

    public static string GetTextFileFromResource(string resourceName,
        string fallbackData = "",
        bool prependWebPanelNamespace = true)
    {
        var assembly = Assembly.GetExecutingAssembly();
        if (prependWebPanelNamespace)
            resourceName = $"XeniaBot.WebPanel.{resourceName}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return fallbackData;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    

    public static List<TSource> Paginate<TSource, TKey>(IEnumerable<TSource> data, Func<TSource, TKey> keySelector, int page = 1, int pageSize = 10)
    {
        return Paginate(data, v => v.OrderBy(keySelector), page, pageSize);
    }

    public static List<TSource> Paginate<TSource>(IEnumerable<TSource> data,
        Func<IEnumerable<TSource>, IEnumerable<TSource>> logic,
        int page = 1,
        int pageSize = 10)
    {
        return logic(data)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}
