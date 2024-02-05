using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using XeniaBot.Shared.Models;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared;

public class HealthServer
{
    private readonly CoreContext _core;
    public HealthServer(CoreContext core)
    {
        _core = core;
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
        var data = new XeniaHealthModel()
        {
            StartTimestamp = _core.Details.StartTimestamp,
            Version = _core.Details.Version,
            ServiceName = "XeniaDiscordBot"
        };
        var json = JsonSerializer.Serialize(data, CoreContext.SerializerOptions);
        return json;
    }
}