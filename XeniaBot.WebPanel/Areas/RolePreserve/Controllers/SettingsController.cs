using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace XeniaBot.WebPanel.Areas.RolePreserve.Controllers;

[Authorize]
[AuthRequired]
[Area("RolePreserve")]
[Route("~/Guild/{guildId}/[area]/[controller]")]
[RestrictToGuild(GuildIdRouteKey = "guildId")]
public class SettingsController : Controller
{
    [Route("", Name = "Guild_RolePreserve_Settings_Index")]
    public async Task<IActionResult> Index(ulong guildId)
    {
        // TODO implement logic to add/remove log channel, if to enable role preservation, and blacklisted roles with role preservation
        throw new NotImplementedException();
    }
}
