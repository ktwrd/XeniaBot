using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    public async Task<ServerDetailsViewModel> GetDetails(ulong serverId)
    {
        var data = new ServerDetailsViewModel();
        var guild = _discord.GetGuild(serverId);
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
        
        return data;
    }
    public bool CanAccess(ulong id)
    {
        bool isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth)
            return false;
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
        {
            return false;
        }
        var user = _discord.GetUser((ulong)userId);
        if (user == null)
            return false;

        var guild = _discord.GetGuild(id);
        var guildUser = guild.GetUser(user.Id);
        if (guildUser == null)
            return false;
        if (!guildUser.GuildPermissions.ManageGuild)
            return false;

        return true;
    }


    public IActionResult? ParseChannelId(ulong guildId, string? channel, out ulong outChannel)
    {
        outChannel = 0;
        ulong? channelId = null;
        try
        {
            if (channel == null)
                throw new Exception("Provided ChannelId is null");
            channelId = ulong.Parse(channel);
        }
        catch (Exception e)
        {
            return RedirectToAction("Index", new
            {
                Id = guildId,
                MessageType = "danger",
                Message = $"Failed to parse ChannelId.<br/><pre><code>{e.Message}</code></pre>"
            });
        }

        if (channelId == null)
        {
            return RedirectToAction("Index", new
            {
                Id = guildId,
                MessageType = "danger",
                Message = $"Failed to parse ChannelId.<br/><pre><code>ChannelId is null</code></pre>"
            });
        }
        else
        {
            outChannel = (ulong)channelId;
            return null;
        }

    }
}