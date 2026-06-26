using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.MongoData.Models;
using XeniaBot.Shared.Helpers;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Models.ServerLog;
using RolePreserveGuildModel = XeniaDiscord.Data.Models.RolePreserve.RolePreserveGuildModel;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    [NonAction]
    public async Task<ServerDetailsViewModel> GetDetails(ulong guildId)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        var user = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(AspHelper.GetUserId(HttpContext) ?? 0));
        var data = new ServerDetailsViewModel
        {
            Guild = guild,
            User = user,
            CounterConfig = new CounterGuildModel()
            {
                GuildId = guildId
            },
            BanSyncConfig = new BanSyncGuildModel(guildId),
            XpConfig = new LevelSystemConfigModel()
            {
                GuildId = guildId
            },
            LogConfig = new ServerLogGuildModel()
            {
                GuildId = guildId.ToString()
            },
            GreeterConfig = new GuildGreeterConfigModel()
            {
                GuildId = guildId
            },
            GreeterGoodbyeConfig = new GuildByeGreeterConfigModel()
            {
                GuildId = guildId
            },
            RolePreserve = new RolePreserveGuildModel(guildId),
            WarnStrikeConfig = new GuildConfigWarnStrikeModel()
            {
                GuildId = guildId
            },
            ConfessionConfig = new ConfessionGuildModel()
            {
                GuildId = guildId
            }
        };
        
        await AspHelper.FillServerModel(HttpContext.RequestServices, guildId, data);
        
        return data;
    }
}