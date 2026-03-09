using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.WebPanel.Models.Component;

public class AdminBanSyncComponentViewModel : IGuildViewModel, IBanSyncViewModel, IAlertViewModel
{
    public SocketGuild Guild { get; set; }
    public BanSyncGuildModel BanSyncConfig { get; set; }
    public ICollection<BanSyncGuildSnapshotModel> BanSyncStateHistory { get; set; } = [];
    
    public string? Message { get; set; }
    public string? MessageType { get; set; }

    public async Task PopulateModel(HttpContext context, ulong guildId)
    {
        var discord = context.RequestServices.GetRequiredService<DiscordSocketClient>();
        Guild = discord.GetGuild(guildId);
        
        var banSyncConfig = context.RequestServices.GetRequiredService<BanSyncGuildRepository>();
        BanSyncConfig = await banSyncConfig.GetAsync(guildId) ?? new BanSyncGuildModel()
        {
            GuildId = guildId.ToString()
        };
        var banSyncStateHistory = context.RequestServices.GetRequiredService<BanSyncGuildSnapshotRepository>();
        BanSyncStateHistory = await banSyncStateHistory.GetMany(guildId);
    }
}