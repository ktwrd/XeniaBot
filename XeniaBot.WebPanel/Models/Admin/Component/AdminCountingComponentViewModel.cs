using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Models.Component;

public class AdminCountingComponentViewModel : IGuildViewModel, IAlertViewModel, ICountingViewModel
{
    public SocketGuild Guild { get; set; }
    public CounterGuildModel CounterConfig { get; set; }
    public string? Message { get; set; }
    public string? MessageType { get; set; }
    
    
    public async Task PopulateModel(HttpContext context, ulong guildId)
    {
        var discord = CoreContext.Instance!.GetRequiredService<DiscordSocketClient>();
        Guild = discord.GetGuild(guildId);
        var repo = CoreContext.Instance!.GetRequiredService<CounterConfigRepository>();
        CounterConfig = await repo.Get(Guild) ?? new CounterGuildModel()
        {
            GuildId = Guild.Id
        };
    }
}