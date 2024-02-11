using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;


public class GuildBannerViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(GuildBannerViewParameters param)
    {
        var client = Program.Core.GetRequiredService<DiscordSocketClient>();
        var guild = client.GetGuild(param.GuildId);
        var data = StrippedGuild.FromGuild(guild);
        var model = new GuildBannerViewModel()
        {
            Guild = data,
            Parameters = param
        };
        return View("Default", model);
    }
}