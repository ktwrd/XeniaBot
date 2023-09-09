﻿using Discord.WebSocket;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public class AdminServerModel : BaseViewModel, IBaseServerModel
{
    public SocketGuild Guild { get; set; }
    public IEnumerable<SocketGuildUser> UsersWhoCanAccess { get; set; }
    
    public CounterGuildModel CounterConfig { get; set; }
    public ConfigBanSyncModel BanSyncConfig { get; set; }
    public LevelSystemGuildConfigModel XpConfig { get; set; }
    public ServerLogModel LogConfig { get; set; }
    public IEnumerable<BanSyncStateHistoryItemModel> BanSyncStateHistory { get; set; }
    public GuildGreeterConfigModel GreeterConfig { get; set; }
}