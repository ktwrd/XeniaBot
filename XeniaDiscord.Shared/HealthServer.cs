using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using XeniaBot.Shared.Models;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared;

public class HealthServer
{
    private readonly CoreContext _core;
    private readonly string? _platformTag;
    public HealthServer(CoreContext core, string? platformTag)
    {
        _core = core;
        _platformTag = platformTag;
    }
    public void Run(int port)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(
            serverOptions =>
            {
                serverOptions.ListenAnyIP(port);
            });
        var app = builder.Build();
        app.MapGet("/", Handle);
        app.Run();
    }

    public string Handle()
    {
        var s = "XeniaDiscordBot";
        if (_platformTag != null)
        {
            s += _platformTag;
        }
        var data = new XeniaHealthModel()
        {
            StartTimestamp = _core.Details.StartTimestamp,
            Version = _core.Details.Version,
            ServiceName = s
        };
        var json = JsonSerializer.Serialize(data, CoreContext.SerializerOptions);
        return json;
    }
}