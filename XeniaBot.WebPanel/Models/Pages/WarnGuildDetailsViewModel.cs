﻿using System;
using System.Collections.Generic;
using Discord.WebSocket;
using XeniaBot.Data.Models;
using XeniaBot.Data.Services;

namespace XeniaBot.WebPanel.Models;

public class WarnGuildDetailsViewModel : BaseViewModel, IBaseServerModel
{
    public SocketGuildUser User { get; set; }
    public SocketGuild Guild { get; set; }
    public ICollection<SocketGuildUser> UsersWhoCanAccess { get; set; }
    
    public CounterGuildModel CounterConfig { get; set; }
    public ConfigBanSyncModel BanSyncConfig { get; set; }
    public LevelSystemConfigModel XpConfig { get; set; }
    public ServerLogModel LogConfig { get; set; }
    public ICollection<BanSyncStateHistoryItemModel> BanSyncStateHistory { get; set; }
    public GuildGreeterConfigModel GreeterConfig { get; set; }
    public GuildByeGreeterConfigModel GreeterGoodbyeConfig { get; set; }
    public ICollection<GuildWarnItemModel> WarnItems { get; set; }
    public RolePreserveGuildModel RolePreserve { get; set; }
    public long BanSyncRecordCount { get; set; }
    public GuildConfigWarnStrikeModel WarnStrikeConfig { get; set; }
    public ConfessionGuildModel ConfessionConfig { get; set; }

    public bool EnableNewerThanFilter { get; set; } = false;

    /// <summary>
    /// YYYY/MM/DD
    /// </summary>
    public string NewerThanDate { get; set; }

    public bool IsWarnActive(GuildWarnItemModel model)
    {
        return WarnStrikeService.IsWarnActive(model, WarnStrikeConfig);
    }
    
    public WarnGuildDetailsViewModel()
        : base()
    {
        var now = DateTimeOffset.UtcNow;
        now = now.AddMonths(-1);
        NewerThanDate = $"{now.Year}/{now.Month}/{now.Day}";
    }
}