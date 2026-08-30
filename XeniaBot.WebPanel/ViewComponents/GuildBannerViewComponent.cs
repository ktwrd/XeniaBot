using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Shared.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;


public class GuildBannerViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(GuildBannerViewParameters param)
    {
        var client = Program.Core.GetRequiredService<DiscordShardedClient>();
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