using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Models.Component;

public class LevelSystemComponentViewModel : IGuildViewModel, ILevelSystemViewModel
{
    public SocketGuild Guild { get; set; }
    public LevelSystemConfigModel XpConfig { get; set; }
    
    public string? Message { get; set; }
    public string? MessageType { get; set; }
    public string MessageClass => MessageType == null ? "alert " : $"alert alert-{MessageType}";
    
    public async Task PopulateModel(HttpContext context, ulong guildId)
    {
        var discord = CoreContext.Instance!.GetRequiredService<DiscordSocketClient>();
        Guild = discord.GetGuild(guildId);
        var xpConfig = CoreContext.Instance!.GetRequiredService<LevelSystemConfigRepository>();
        XpConfig = await xpConfig.Get(Guild.Id) ?? new LevelSystemConfigModel()
        {
            GuildId = Guild.Id
        };
    }
}