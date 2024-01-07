using System.Diagnostics;
using System.Reflection;
using Discord.WebSocket;
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
        return Program.ConfigData.UserWhitelist.Contains(userId);
    }

    public static bool CanAccessGuild(ulong guildId, ulong userId)
    {
        var discord = Program.Services.GetRequiredService<DiscordSocketClient>();
        
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
    public static async Task<T> FillServerModel<T>(ulong serverId, T data) where T : IBaseServerModel
    {
        
        var discord = Program.Services.GetRequiredService<DiscordSocketClient>();
        var guild = discord.GetGuild(serverId);
        data.Guild = guild;

        var counterController = Program.Services.GetRequiredService<CounterConfigController>();
        data.CounterConfig = await counterController.Get(guild) ?? new CounterGuildModel()
        {
            GuildId = serverId
        };

        var banSyncConfig = Program.Services.GetRequiredService<BanSyncConfigController>();
        data.BanSyncConfig = await banSyncConfig.Get(guild.Id) ?? new ConfigBanSyncModel()
        {
            GuildId = guild.Id
        };

        var banSyncStateHistory = Program.Services.GetRequiredService<BanSyncStateHistoryConfigController>();
        data.BanSyncStateHistory = await banSyncStateHistory.GetMany(guild.Id) ?? Array.Empty<BanSyncStateHistoryItemModel>();

        var xpConfig = Program.Services.GetRequiredService<LevelSystemGuildConfigController>();
        data.XpConfig = await xpConfig.Get(guild.Id) ?? new LevelSystemGuildConfigModel()
        {
            GuildId = guild.Id
        };

        var logConfig = Program.Services.GetRequiredService<ServerLogConfigController>();
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

        var greeterConfig = Program.Services.GetRequiredService<GuildGreeterConfigController>();
        data.GreeterConfig = await greeterConfig.GetLatest(guild.Id)
            ?? new GuildGreeterConfigModel()
            {
                GuildId = guild.Id
            };

        var greeterGoodbyeConfig = Program.Services.GetRequiredService<GuildGreetByeConfigController>();
        data.GreeterGoodbyeConfig = await greeterGoodbyeConfig.GetLatest(guild.Id)
            ?? new GuildByeGreeterConfigModel()
            {
                GuildId = guild.Id
            };

        var warnConfig = Program.Services.GetRequiredService<GuildWarnItemConfigController>();
        data.WarnItems = await warnConfig.GetLatestGuildItems(guild.Id) ?? new List<GuildWarnItemModel>();

        var rolePreserveConfig = Program.Services.GetRequiredService<RolePreserveGuildConfigController>();
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