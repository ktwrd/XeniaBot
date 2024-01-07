using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public class WarnGuildDetailsViewModel : BaseViewModel, IBaseServerModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    public ICollection<SocketGuildUser> UsersWhoCanAccess { get; set; }
    
    public CounterGuildModel CounterConfig { get; set; }
    public ConfigBanSyncModel BanSyncConfig { get; set; }
    public LevelSystemGuildConfigModel XpConfig { get; set; }
    public ServerLogModel LogConfig { get; set; }
    public ICollection<BanSyncStateHistoryItemModel> BanSyncStateHistory { get; set; }
    public GuildGreeterConfigModel GreeterConfig { get; set; }
    public GuildByeGreeterConfigModel GreeterGoodbyeConfig { get; set; }
    public ICollection<GuildWarnItemModel> WarnItems { get; set; }
    public RolePreserveGuildModel RolePreserve { get; set; }
}