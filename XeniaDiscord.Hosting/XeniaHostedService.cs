using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaDiscord.Hosting;

public class XeniaHostedService(IServiceProvider services, ILogger<XeniaHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var objectSerializer = new ObjectSerializer(type
            => ObjectSerializer.DefaultAllowedTypes(type)
               || type.FullName?.StartsWith("XeniaBot", StringComparison.OrdinalIgnoreCase) == true
               || type.FullName?.StartsWith("XeniaDiscord", StringComparison.OrdinalIgnoreCase) == true);
        BsonSerializer.RegisterSerializer(objectSerializer);

        RunServiceInit();
        
        var discord = services.GetRequiredService<DiscordService>();
        discord.Ready += DiscordServiceOnReady;
        await discord.Run();
        // await Task.Delay(-1, cancellationToken);
    }

    private void RunServiceInit()
    {
        using var trans = SentryHelper.CreateTransaction();
        try
        {
            AllBaseServices(item => item.InitializeAsync());
            trans.Finish();
            logger.LogInformation("Done");
        }
        catch (Exception ex)
        {
            trans.Finish(ex);
            throw;
        }
    }
    private async void DiscordServiceOnReady(DiscordService service)
    {
        try
        {
            DiscordRunServiceReady();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to invoke RunServiceReady");
        }

        await Task.Delay(2000);
        
        try
        {
            RunServiceDelayedReady();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to invoke RunServiceDelayedReady");
        }
    }

    private void DiscordRunServiceReady()
    {
        using var trans = SentryHelper.CreateTransaction();
        try
        {
            AllBaseServices(static svc => svc.OnReady());
            trans.Finish();
            logger.LogInformation("Done - Bot is online!");
        }
        catch (Exception ex)
        {
            trans.Finish(ex);
            throw;
        }
    }
    private void RunServiceDelayedReady()
    {
        using var trans = SentryHelper.CreateTransaction();
        try
        {
            AllBaseServices(svc => svc.OnReadyDelay());
            trans.Finish();
            logger.LogInformation("Done");
        }
        catch (Exception ex)
        {
            trans.Finish(ex);
            throw;
        }
    }
    /// <summary>
    /// For every registered class that extends <see cref="IBaseService"/>,
    /// call <paramref name="func"/> with the argument as the target service.
    /// </summary>
    private void AllBaseServices(Func<IBaseService, Task> func)
    {
        Task.WhenAll(services.GetServices<IBaseService>()
                .OrderBy(v => v.Priority)
                .ThenBy(v => v.GetType().AssemblyQualifiedName)
                .Select(ProcessItem))
            .GetAwaiter().GetResult();
        async Task ProcessItem(IBaseService svc)
        {
            await func(svc);
        }
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var discord = services.GetRequiredService<DiscordService>();
        discord.Ready -= DiscordServiceOnReady;
        await discord.Stop();
    }
}