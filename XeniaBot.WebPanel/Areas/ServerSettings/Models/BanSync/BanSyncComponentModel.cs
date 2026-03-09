using Discord.WebSocket;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaBot.WebPanel.Areas.ServerSettings.Models.BanSync;

public class BanSyncComponentModel
{
    public required SocketGuild Guild { get; set; }
    public required BanSyncGuildModel BanSyncGuild { get; set; }
    public AlertComponentViewModel? Alert { get; set; }
}
