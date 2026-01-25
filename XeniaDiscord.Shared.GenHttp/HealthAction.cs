using System.Reflection;
using System.Text.Json;
using Discord;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.IO;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.WebApi.Models;

namespace XeniaDiscord.Shared.GenHttp;

public class HealthAction : BaseActionItem
{
    private readonly IDiscordClient _discord;

    public HealthAction(IServiceProvider services)
    {
        _discord = services.GetRequiredService<IDiscordClient>();
    }

    public override bool OnPredicate(IRequest request)
    {
        return request.Target.Path.ToString()?.Equals("/api/v1/health", StringComparison.InvariantCultureIgnoreCase) ?? false;
    }

    public override async Task<IResponse?> OnRequest(IRequest request)
    {
        return request.Respond()
            .Status(ResponseStatus.Ok)
            .Content(GetJson())
            .Type(new FlexibleContentType(ContentType.ApplicationJson))
            .Build();
    }
    public string GetJson()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        var result = new HealthResponse()
        {
            Version = version ?? "unknown",
            UserId = _discord.CurrentUser.Id,
            UserDisplayName = _discord.CurrentUser.GlobalName
        };
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var asmName = asm.GetName();
            if (string.IsNullOrEmpty(asmName.Name))
                continue;
            if (!(asmName.Name?.ToLower()?.StartsWith("xeniadiscord") ?? false))
                continue;

            var moduleVersion = asmName.Version;
            result.Modules.Add(asmName.Name, moduleVersion?.ToString() ?? "unknown");
        }
        result.Modules = result.Modules.OrderBy(e => e.Key).ToDictionary();

        return JsonSerializer.Serialize(result, serializerOptions);
    }
    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        WriteIndented = true
    };
}
