using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Models.Component;

public class AdminLevelSystemComponentViewModel : IGuildViewModel, IAlertViewModel, ILevelSystemViewModel
{
    public SocketGuild Guild { get; set; }
    public LevelSystemConfigModel XpConfig { get; set; }
    
    public string? Message { get; set; }
    public string? MessageType { get; set; }
    
    public async Task PopulateModel(HttpContext context, ulong guildId)
    {
        var discord = context.RequestServices.GetRequiredService<DiscordShardedClient>();
        var xpConfig = context.RequestServices.GetRequiredService<LevelSystemConfigRepository>();
        Guild = ExceptionHelper.RetryOnTimedOut(() => discord.GetGuild(guildId));
        XpConfig = await xpConfig.Get(Guild.Id) ?? new LevelSystemConfigModel()
        {
            GuildId = Guild.Id
        };
    }
}