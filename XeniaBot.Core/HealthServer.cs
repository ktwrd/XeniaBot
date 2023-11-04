using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using XeniaBot.Shared.Models;

namespace XeniaBot.Core;

public class HealthServer
{
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
            StartTimestamp = Program.StartTimestamp,
            Version = Program.Version,
            ServiceName = "XeniaDiscordBot"
        };
        var json = JsonSerializer.Serialize(data, Program.SerializerOptions);
        return json;
    }
}