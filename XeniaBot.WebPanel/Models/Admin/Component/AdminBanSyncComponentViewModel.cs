using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Models.Component;

public class AdminBanSyncComponentViewModel : IGuildViewModel, IBanSyncViewModel, IAlertViewModel
{
    public SocketGuild Guild { get; set; }
    public ConfigBanSyncModel BanSyncConfig { get; set; }
    public ICollection<BanSyncStateHistoryItemModel> BanSyncStateHistory { get; set; }
    
    public string? Message { get; set; }
    public string? MessageType { get; set; }

    public async Task PopulateModel(HttpContext context, ulong guildId)
    {
        var discord = CoreContext.Instance!.GetRequiredService<DiscordSocketClient>();
        Guild = discord.GetGuild(guildId);
        
        var banSyncConfig = Program.Core.GetRequiredService<BanSyncConfigRepository>();
        BanSyncConfig = await banSyncConfig.Get(guildId) ?? new ConfigBanSyncModel()
        {
            GuildId = guildId
        };
        var banSyncStateHistory = CoreContext.Instance!.GetRequiredService<BanSyncStateHistoryRepository>();
        BanSyncStateHistory = await banSyncStateHistory.GetMany(guildId) ?? Array.Empty<BanSyncStateHistoryItemModel>();
    }
}