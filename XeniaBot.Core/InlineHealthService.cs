using GenHTTP.Modules.Practices;
using GenHTTP.Modules.Security;
using NLog;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using XeniaBot.Shared.Config;
using XeniaDiscord.Shared.GenHttp;
using Host = GenHTTP.Engine.Internal.Host;

namespace XeniaBot.Core;

public static class InlineHealthService
{
    private static Logger Log => LogManager.GetLogger("HealthServer");
    public static void RunThread(IServiceProvider services)
    {
        var thread = new Thread(() => ThreadDelegate(services).Wait())
        {
            Name = "XeniaDiscord.ShardedBot.HealthServer"
        };
        thread.Start();
        Log.Debug($"Created thread (id: {thread.ManagedThreadId})");
    }
    private static async Task ThreadDelegate(IServiceProvider services)
    {
        try
        {
            var cfg = XeniaConfig.Get();
            var handler = GetHandler(services);
            var host = Host.Create()
                .Handler(handler)
                .Defaults();
            if (!IPAddress.TryParse(cfg.Health.Address, out var ipaddr) && !string.IsNullOrEmpty(cfg.Health.Address))
            {
                Log.Warn($"Failed to parse Health Server Address \"{cfg.Health.Address}\"");
            }
            if (cfg.Health.SslConfig.Enabled)
            {
                var cert = cfg.Health.SslConfig.GetCertificate();
                host = host.Bind(ipaddr, cfg.Health.Port, cert);
            }
            else
            {
                host = string.IsNullOrEmpty(cfg.Health.Address)
                    ? host.Port(cfg.Health.Port)
                    : host.Bind(ipaddr, cfg.Health.Port);
            }
            var task = host.RunAsync();
            Log.Info("Running on port: " + cfg.Health.Port);
            var result = await task;
            Log.Warn("Health Server exited with code: " + result);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
    private static DynamicEndpointBuilder GetHandler(IServiceProvider services)
    {
        return new DynamicEndpointBuilder(services)
            .Add(new HealthAction(services))
            .Add(CorsPolicy.Permissive());
    }
}
