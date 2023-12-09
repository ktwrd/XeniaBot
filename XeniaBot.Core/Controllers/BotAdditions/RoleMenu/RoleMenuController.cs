using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers.BotAdditions;

[BotController]
public class RoleMenuController : BaseController
{
    private readonly DiscordSocketClient _discord;
    private readonly RoleMenuManagerController _manager;
    private readonly RoleMenuConfigController _menuConfig;
    private readonly RoleMenuSelectConfigController _selectConfig;
    private readonly RoleMenuOptionConfigController _optionConfig;
    

    public RoleMenuController(IServiceProvider services)
        : base(services)
    {
        _manager = services.GetRequiredService<RoleMenuManagerController>();
        _menuConfig = services.GetRequiredService<RoleMenuConfigController>();
        _selectConfig = services.GetRequiredService<RoleMenuSelectConfigController>();
        _optionConfig = services.GetRequiredService<RoleMenuOptionConfigController>();
        
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _discord.SelectMenuExecuted += DiscordOnSelectMenuExecuted;
    }

    private async Task DiscordOnSelectMenuExecuted(SocketMessageComponent arg)
    {
        var menu = await _menuConfig.GetLatestByMessageId(arg.Message.Id);
        if (menu == null)
            return;

        Debugger.Break();


        var roleGrant = new List<ulong>();
        var roleRevoke = new List<ulong>();
    }
}