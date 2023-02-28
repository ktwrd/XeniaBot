using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Controllers.BotAdditions
{
    [SkidController]
    public class RoleController : BaseController
    {
        private DiscordSocketClient _client;
        public RoleController(IServiceProvider services)
            : base (services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
        }

        
    }
}
