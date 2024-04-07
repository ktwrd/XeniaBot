using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Models.Component;

public class AdminRolePreserveComponentViewModel : IGuildViewModel, IAlertViewModel, IRolePreserveViewModel
{
    public SocketGuild Guild { get; set; }
    public RolePreserveGuildModel RolePreserve { get; set; }
    public string? Message { get; set; }
    public string? MessageType { get; set; }
    public async Task PopulateModel(HttpContext context, ulong guildId)
    {
        var discord = CoreContext.Instance!.GetRequiredService<DiscordSocketClient>();
        Guild = discord.GetGuild(guildId);
        var repo = CoreContext.Instance!.GetRequiredService<RolePreserveGuildRepository>();
        RolePreserve = await repo.Get(Guild.Id) ?? new RolePreserveGuildModel()
        {
            GuildId = Guild.Id
        };
    }
}