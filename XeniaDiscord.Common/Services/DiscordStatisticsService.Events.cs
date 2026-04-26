using Discord.WebSocket;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaDiscord.Common.Services;

partial class DiscordStatisticsService
{
    private Task OnSlashCommandExecuted(SocketSlashCommand interaction)
    {
        if (!_configData.Prometheus.Enable || _details.Platform != XeniaPlatform.Bot) return Task.CompletedTask;
        IncreaseEvent(DiscordStatisticsEventType.SlashCommandExecuted);

        var trans = SentryHelper.CreateTransaction();
        new Thread(() =>
        {
            try
            {
                HandleSlashCommandExecuted(interaction).GetAwaiter().GetResult();
                trans.Finish();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to make metrics");
                trans.Finish(ex);
            }
        })
        {
            Name = $"{nameof(DiscordStatisticsService)}.{nameof(HandleSlashCommandExecuted)}"
        }.Start();
        return Task.CompletedTask;
    }

    private Task OnMessageReceived(SocketMessage message)
    {
        if (!_configData.Prometheus.Enable || _details.Platform != XeniaPlatform.Bot) return Task.CompletedTask;
        IncreaseEvent(DiscordStatisticsEventType.MessageReceived);

        var trans = SentryHelper.CreateTransaction();
        new Thread(() =>
        {
            try
            {
                HandleMessageReceived(message).GetAwaiter().GetResult();
                trans.Finish();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to make metrics");
                trans.Finish(ex);
            }
        })
        {
            Name = $"{nameof(DiscordStatisticsService)}.{nameof(HandleMessageReceived)}"
        }.Start();
        return Task.CompletedTask;
    }
}