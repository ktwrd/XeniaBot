using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Services;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Helpers;

public static class AspHelper
{
    public static ulong? GetUserId(HttpContext context)
    {
        ulong? target = null;
        if (context.User?.Identity?.IsAuthenticated ?? false)
        {
            foreach (var claim in context.User.Claims)
            {
                if (claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                {
                    target = ulong.Parse(claim.Value);
                    return target;
                }
            }
        }

        return target;
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
            var user = discord.GetUser(userId);
            if (user == null)
                return false;

            var guild = discord.GetGuild(guildId);
            var guildUser = guild.GetUser(user.Id);
            if (guildUser == null)
                return false;
            if (!guildUser.GuildPermissions.Has(permissionRequired))
                return false;

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to run {guildId}, {userId}, {permissionRequired}\n{ex}");
            errorReport.ReportException(
                ex, $"Failed to run AspHelper.CanAccessGuild ({guildId}, {userId}, {permissionRequired})");
            return false;
        }
    }
    public static string[] ValidMessageTypes = new string[]
    {
        "primary",
        "secondary",
        "success",
        "danger",
        "warning",
        "info"
    };

    public static string GetUserProfilePicture(ulong userId)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var user = discord.GetUser(userId);
        if (user == null)
            return "/Debugempty.png";

        var v = user.GetAvatarUrl();
        v ??= user.GetDefaultAvatarUrl();
        v ??= "/Debugempty.png";
        return v;
    }

    public static string GetUserProfilePicture(SocketGuildUser guildUser)
    {
        return guildUser.GetGuildAvatarUrl() ?? GetUserProfilePicture(guildUser.Id);
    }

    public static string GetGuildImage(ulong guildId)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = discord.GetGuild(guildId);
        if (guild == null)
            return "/Debugempty.png";

        var s = guild.IconUrl ?? "/Debugempty.png";
        return s;
    }

    public static string GetGuildName(ulong guildId)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = discord.GetGuild(guildId);
        if (guild == null)
            return guildId.ToString();

        return guild.Name;
    }

    public static string GetChannelName(ulong guildId, ulong channelId)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = discord.GetGuild(guildId);
        if (guild == null)
            return channelId.ToString();

        foreach (var i in guild.Channels)
        {
            if (i.Id == channelId)
                return i.Name;
        }

        return channelId.ToString();
    }
    
    public static async Task<ServerBanSyncViewModel> FillServerModel(ulong serverId, ServerBanSyncViewModel data)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = discord.GetGuild(serverId);
        data.Guild = guild;
        
        var banSyncRecordConfig = Program.Core.GetRequiredService<BanSyncInfoRepository>();
        data.BanSyncRecords = await banSyncRecordConfig.GetInfoAllInGuild(serverId);

        var banSyncGuildConfig = Program.Core.GetRequiredService<BanSyncStateHistoryRepository>();
        data.BanSyncGuild = await banSyncGuildConfig.GetLatest(serverId) ?? new BanSyncStateHistoryItemModel()
        {
            GuildId = serverId
        };

        return data;
    }
    public static async Task<T> FillServerModel<T>(ulong serverId, T data) where T : IBaseServerModel
    {
        
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = discord.GetGuild(serverId);
        data.Guild = guild;

        var counterController = Program.Core.GetRequiredService<CounterConfigRepository>();
        data.CounterConfig = await counterController.Get(guild) ?? new CounterGuildModel()
        {
            GuildId = serverId
        };

        var banSyncConfig = Program.Core.GetRequiredService<BanSyncConfigRepository>();
        data.BanSyncConfig = await banSyncConfig.Get(guild.Id) ?? new ConfigBanSyncModel()
        {
            GuildId = guild.Id
        };

        var banSyncStateHistory = Program.Core.GetRequiredService<BanSyncStateHistoryRepository>();
        data.BanSyncStateHistory = await banSyncStateHistory.GetMany(guild.Id) ?? Array.Empty<BanSyncStateHistoryItemModel>();

        var xpConfig = Program.Core.GetRequiredService<LevelSystemConfigRepository>();
        data.XpConfig = await xpConfig.Get(guild.Id) ?? new LevelSystemConfigModel()
        {
            GuildId = guild.Id
        };

        var logConfig = Program.Core.GetRequiredService<ServerLogRepository>();
        data.LogConfig = await logConfig.Get(guild.Id) ?? new ServerLogModel()
        {
            ServerId = guild.Id
        };

        var membersWhoCanAccess = new List<SocketGuildUser>();
        foreach (var item in guild.Users)
        {
            if (CanAccessGuild(guild.Id, item.Id) && !item.IsBot)
                membersWhoCanAccess.Add(item);
        }
        data.UsersWhoCanAccess = membersWhoCanAccess;

        var greeterConfig = Program.Core.GetRequiredService<GuildGreeterConfigRepository>();
        data.GreeterConfig = await greeterConfig.GetLatest(guild.Id)
            ?? new GuildGreeterConfigModel()
            {
                GuildId = guild.Id
            };

        var greeterGoodbyeConfig = Program.Core.GetRequiredService<GuildGreetByeConfigRepository>();
        data.GreeterGoodbyeConfig = await greeterGoodbyeConfig.GetLatest(guild.Id)
            ?? new GuildByeGreeterConfigModel()
            {
                GuildId = guild.Id
            };

        var warnConfig = Program.Core.GetRequiredService<GuildWarnItemRepository>();
        data.WarnItems = await warnConfig.GetLatestGuildItems(guild.Id) ?? new List<GuildWarnItemModel>();

        var rolePreserveConfig = Program.Core.GetRequiredService<RolePreserveGuildRepository>();
        data.RolePreserve = await rolePreserveConfig.Get(guild.Id) ?? new RolePreserveGuildModel()
        {
            GuildId = guild.Id
        };

        var banSyncRecordService = Program.Core.GetRequiredService<BanSyncInfoRepository>();
        data.BanSyncRecordCount = await banSyncRecordService.CountInGuild(guild.Id);

        var warnStrikeService = Program.Core.GetRequiredService<WarnStrikeService>();
        data.WarnStrikeConfig = await warnStrikeService.GetStrikeConfig(guild.Id);
        
        return data;
    }

    public static DateTime DateTimeFromTimestamp(long timestamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddMilliseconds( timestamp ).ToLocalTime();
        return dateTime;
    }

    public static string GetTextFileFromResource(string resourceName,
        string fallbackData = "",
        bool prependWebPanelNamespace = true)
    {
        var assembly = Assembly.GetExecutingAssembly();
        if (prependWebPanelNamespace)
            resourceName = $"XeniaBot.WebPanel.{resourceName}";
        using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
                return fallbackData;
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
    

    public static List<TSource> Paginate<TSource, TKey>(IEnumerable<TSource> data, Func<TSource, TKey> keySelector, int page = 1, int pageSize = 10)
    {
        return data.OrderBy(keySelector)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}