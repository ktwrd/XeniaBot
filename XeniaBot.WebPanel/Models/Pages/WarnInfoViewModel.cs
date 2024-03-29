using System.Collections.Generic;
using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public class WarnInfoViewModel : BaseViewModel
{
    public SocketGuild Guild { get; set; }
    
    public GuildWarnItemModel WarnItem { get; set; }
    public ICollection<GuildWarnItemModel> History { get; set; }
}