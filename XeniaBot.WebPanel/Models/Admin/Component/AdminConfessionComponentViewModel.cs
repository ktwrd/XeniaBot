using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Models.Component;

public class AdminConfessionComponentViewModel : IGuildViewModel, IConfessionViewModel, IAlertViewModel
{
    public SocketGuild Guild { get; set; }
    public ConfessionGuildModel ConfessionModel { get; set; }
    
    public string? Message { get; set; }
    public string? MessageType { get; set; }
    
    
    public async Task PopulateModel(HttpContext context, ulong guildId)
    {
        var discord = CoreContext.Instance!.GetRequiredService<DiscordSocketClient>();
        Guild = discord.GetGuild(guildId);
        var repo = CoreContext.Instance!.GetRequiredService<ConfessionConfigRepository>();
        ConfessionModel = await repo.GetGuild(Guild.Id) ?? new ConfessionGuildModel()
        {
            GuildId = Guild.Id
        };
    }
}