using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Shared.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;


public class GuildBannerViewComponent(DiscordShardedClient client) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(GuildBannerViewParameters param)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => client.GetGuild(param.GuildId));
        var data = StrippedGuild.FromGuild(guild);
        var model = new GuildBannerViewModel()
        {
            Guild = data,
            Parameters = param
        };
        return View("Default", model);
    }
}