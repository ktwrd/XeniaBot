using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data.Models.RolePreserve;
using XeniaDiscord.Data.Repositories;

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
        var repo = context.RequestServices.GetRequiredService<RolePreserveGuildRepository>();
        Guild = discord.GetGuild(guildId);
        RolePreserve = await repo.GetAsync(Guild.Id) ?? new RolePreserveGuildModel(Guild.Id);
    }
}