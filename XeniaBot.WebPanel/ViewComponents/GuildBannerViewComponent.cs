using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class GuildBannerViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(ulong guildId)
    {
        var client = Program.Services.GetRequiredService<DiscordSocketClient>();
        var guild = client.GetGuild(guildId);
        var data = StrippedGuild.FromGuild(guild);
        return View("Default", data);
    }
}