using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Controllers.Wrappers;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers.BotAdditions;

[BotController]
public class AttachmentController : BaseController
{
    private readonly DiscordSocketClient _discord;
    private readonly ConfigData _configData;
    private readonly GoogleCloudStorageController _gcs;
    private readonly AttachmentArchiveConfigController _archiveConfigController;
    public AttachmentController(IServiceProvider services)
        : base(services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _gcs = services.GetRequiredService<GoogleCloudStorageController>();
        _configData = services.GetRequiredService<ConfigData>();
        _archiveConfigController = services.GetRequiredService<AttachmentArchiveConfigController>();
        _discord.MessageReceived += DiscordOnMessageReceived;
    }

    private async Task DiscordOnMessageReceived(SocketMessage arg)
    {
        if (arg.Channel is SocketGuildChannel guildChannel)
        {
            
        }
    }
}