﻿using System.Diagnostics;
using System.Reflection;
using Discord.WebSocket;
using XeniaBot.Data.Controllers;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
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

    public static bool CanAccessGuild(ulong guildId, ulong userId)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        
        var user = discord.GetUser(userId);
        if (user == null)
            return false;

        var guild = discord.GetGuild(guildId);
        var guildUser = guild.GetUser(user.Id);
        if (guildUser == null)
            return false;
        if (!guildUser.GuildPermissions.ManageGuild)
            return false;

        return true;
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

    public static string GetGuildImage(ulong guildId)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = discord.GetGuild(guildId);
        if (guild == null)
            return "/Debugempty.png";

        var s = guild.IconUrl ?? "/Debugempty.png";
        return s;
    }
    
    public static async Task<ServerBanSyncViewModel> FillServerModel(ulong serverId, ServerBanSyncViewModel data)
    {
        var discord = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = discord.GetGuild(serverId);
        data.Guild = guild;
        
        var banSyncRecordConfig = Program.Core.GetRequiredService<BanSyncInfoConfigController>();
        data.BanSyncRecords = await banSyncRecordConfig.GetInfoAllInGuild(serverId);

        var banSyncGuildConfig = Program.Core.GetRequiredService<BanSyncStateHistoryConfigController>();
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

        var counterController = Program.Core.GetRequiredService<CounterConfigController>();
        data.CounterConfig = await counterController.Get(guild) ?? new CounterGuildModel()
        {
            GuildId = serverId
        };

        var banSyncConfig = Program.Core.GetRequiredService<BanSyncConfigController>();
        data.BanSyncConfig = await banSyncConfig.Get(guild.Id) ?? new ConfigBanSyncModel()
        {
            GuildId = guild.Id
        };

        var banSyncStateHistory = Program.Core.GetRequiredService<BanSyncStateHistoryConfigController>();
        data.BanSyncStateHistory = await banSyncStateHistory.GetMany(guild.Id) ?? Array.Empty<BanSyncStateHistoryItemModel>();

        var xpConfig = Program.Core.GetRequiredService<LevelSystemGuildConfigController>();
        data.XpConfig = await xpConfig.Get(guild.Id) ?? new LevelSystemGuildConfigModel()
        {
            GuildId = guild.Id
        };

        var logConfig = Program.Core.GetRequiredService<ServerLogConfigController>();
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

        var greeterConfig = Program.Core.GetRequiredService<GuildGreeterConfigController>();
        data.GreeterConfig = await greeterConfig.GetLatest(guild.Id)
            ?? new GuildGreeterConfigModel()
            {
                GuildId = guild.Id
            };

        var greeterGoodbyeConfig = Program.Core.GetRequiredService<GuildGreetByeConfigController>();
        data.GreeterGoodbyeConfig = await greeterGoodbyeConfig.GetLatest(guild.Id)
            ?? new GuildByeGreeterConfigModel()
            {
                GuildId = guild.Id
            };

        var warnConfig = Program.Core.GetRequiredService<GuildWarnItemConfigController>();
        data.WarnItems = await warnConfig.GetLatestGuildItems(guild.Id) ?? new List<GuildWarnItemModel>();

        var rolePreserveConfig = Program.Core.GetRequiredService<RolePreserveGuildConfigController>();
        data.RolePreserve = await rolePreserveConfig.Get(guild.Id) ?? new RolePreserveGuildModel()
        {
            GuildId = guild.Id
        };
        
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
}