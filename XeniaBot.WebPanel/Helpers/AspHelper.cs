using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using NLog;
using XeniaBot.DiscordCache.Helpers;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Repositories;
using XeniaBot.MongoData.Services;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Data.Repositories;

using RolePreserveGuildRepository = XeniaDiscord.Data.Repositories.RolePreserveGuildRepository;
using RolePreserveGuildModel = XeniaDiscord.Data.Models.RolePreserve.RolePreserveGuildModel;
using ServerLogRepository = XeniaDiscord.Data.Repositories.ServerLogRepository;

namespace XeniaBot.WebPanel.Helpers;

public static class AspHelper
{
    private const string DiscordUserIdClaimType
        = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
    
    public static ulong? GetUserId(HttpContext context)
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        if (context.User?.Identity?.IsAuthenticated != true)
            return null;
        
        var claim = context.User.Claims.FirstOrDefault(e => e.Type == DiscordUserIdClaimType);
        
        if (ulong.TryParse(claim?.Value, out var value))
            return value;
        return null;
    }
    
    public static bool IsCurrentUserAdmin(HttpContext context)
    {
        var userId = GetUserId(context) ?? 0;
        return Program.Core.Config.Data.UserWhitelist.Contains(userId);
    }

    public static bool CanAccessGuild(
        ulong guildId,
        ulong userId,
        GuildPermission permissionRequired = GuildPermission.ManageGuild)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var errorReport = Program.Core.GetRequiredService<ErrorReportService>();
        try
        {
            var user = ExceptionHelper.RetryOnTimedOut(() => discord.GetUser(userId));
            if (user == null)
                return false;

            var guild = ExceptionHelper.RetryOnTimedOut(() => discord.GetGuild(guildId));
            if (guild == null)
                return false;
            
            var guildUser = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(user.Id));
            return guildUser?.GuildPermissions.Has(permissionRequired) == true;
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger()
                .Error(ex, $"Failed to run {guildId}, {userId}, {permissionRequired}");
            errorReport.ReportException(
                ex, $"Failed to run AspHelper.CanAccessGuild ({guildId}, {userId}, {permissionRequired})").GetAwaiter().GetResult();
            return false;
        }
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

    public static string GetUserProfilePicture(ulong userId)
    {
        var user = DiscordCacheHelper.TryGetUser(userId).GetAwaiter().GetResult();
        if (user == null)
        {
            return "/Debugempty.png";
        }
        else
        {
            return user.GetDisplayAvatarUrl() ?? "/Debugempty.png";
        }
    }

    public static string GetUserProfilePicture(SocketGuildUser guildUser)
    {
        return guildUser.GetGuildAvatarUrl()
            ?? GetUserProfilePicture(guildUser.Id);
    }

    public static string GetGuildImage(ulong guildId)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = ExceptionHelper.RetryOnTimedOut(() => discord.GetGuild(guildId));
        if (guild == null)
            return "/Debugempty.png";

        var s = guild.IconUrl ?? "/Debugempty.png";
        return s;
    }

    public static string GetGuildName(ulong guildId)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = ExceptionHelper.RetryOnTimedOut(() => discord.GetGuild(guildId));
        return guild?.Name ?? guildId.ToString();
    }

    public static string GetChannelName(ulong guildId, ulong channelId)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = ExceptionHelper.RetryOnTimedOut(() => discord.GetGuild(guildId));
        if (guild == null)
            return channelId.ToString();

        foreach (var i in guild.Channels)
        {
            if (i.Id == channelId)
                return i.Name;
        }

        return channelId.ToString();
    }

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
        ulong serverId,
        T data)
        where T : IBaseServerModel
    {
        var discord = services.GetRequiredService<DiscordSocketClient>();
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
        foreach (var item in guild.Users)
        {
            if (CanAccessGuild(guild.Id, item.Id) && !item.IsBot)
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
        dateTime = seconds
            ? dateTime.AddSeconds(timestamp).ToLocalTime()
            : dateTime.AddMilliseconds(timestamp).ToLocalTime();
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
    

    public static List<TSource> Paginate<TSource, TKey>(
        IEnumerable<TSource> data,
        Func<TSource, TKey> keySelector,
        int page = 1,
        int pageSize = 10)
    {
        return Paginate(data, v => v.OrderBy(keySelector), page, pageSize);
    }

    public static List<TSource> Paginate<TSource>(
        IEnumerable<TSource> data,
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
