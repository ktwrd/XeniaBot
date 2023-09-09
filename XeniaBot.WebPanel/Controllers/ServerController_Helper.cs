using Discord.WebSocket;
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
        data.User = guild.GetUser(AspHelper.GetUserId(HttpContext) ?? 0);
        
        await AspHelper.FillServerModel(serverId, data);
        
        return data;
    }

    public bool CanAccess(ulong guildId)
    {
        bool isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth)
            return false;
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
        {
            return false;
        }

        return CanAccess(guildId, (ulong)userId);
    }
    public bool CanAccess(ulong guildId, ulong userId)
    {
        var user = _discord.GetUser(userId);
        if (user == null)
            return false;

        var guild = _discord.GetGuild(guildId);
        var guildUser = guild.GetUser(user.Id);
        if (guildUser == null)
            return false;
        if (!guildUser.GuildPermissions.ManageGuild)
            return false;

        return true;
    }
}