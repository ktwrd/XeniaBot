using System.Text.Json;
using Discord.WebSocket;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using XeniaBot.Shared.Models;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared;

public class HealthServer
{
    private readonly ProgramDetails _programDetails;
    private readonly DiscordSocketClient _socketClient;
    private readonly ConfigData _config;

    public HealthServer(
        ProgramDetails programDetails,
        DiscordSocketClient socketClient,
        ConfigData config)
    {
        _programDetails = programDetails;
        _socketClient = socketClient;
        _config = config;
    }

    public void Run()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(
            serverOptions =>
            {
                serverOptions.ListenAnyIP(_config.Health.Port);
            });
        var app = builder.Build();
        app.MapGet("/", Handle);
        app.Run();
    }

    public string Handle()
    {
        var s = "XeniaDiscordBot";
        if (!string.IsNullOrEmpty(_programDetails.PlatformTag))
        {
            s += _programDetails.PlatformTag;
        }
        var data = new XeniaHealthModel()
        {
            StartTimestamp = _programDetails.StartTimestamp,
            Version = _programDetails.Version,
            ServiceName = s,
            Latency = _socketClient.Latency
        };
        var json = JsonSerializer.Serialize(data, CoreContext.SerializerOptions);
        return json;
    }
}