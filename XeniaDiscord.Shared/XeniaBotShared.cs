using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared;

public static class XeniaBotShared
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ErrorReportService>().AddSingleton<ErrorReportService>();
    }
}
