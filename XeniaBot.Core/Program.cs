using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XeniaBot.Shared;
using XeniaBot.Shared.Config;
using XeniaDiscord.Common;
using XeniaDiscord.Shared.Interactions;

namespace XeniaBot.Core;

public static class Program
{
    public static bool IsDevelopment
    {
        get
        {
            return string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "development", StringComparison.InvariantCultureIgnoreCase)
                   || string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "development", StringComparison.InvariantCultureIgnoreCase);
        }
    }
    public static void Main(string[] args)
    {
        var log = LogManager.GetCurrentClassLogger();
        try
        {
            StartupGlue.CheckAll(typeof(Program).Assembly, args);
        }
        catch (Exception ex)
        {
            log.Fatal(ex, "Failed to run StartupGlue!!!");
            Environment.Exit(1);
        }

        IServiceProvider services;
        try
        {
            services = GetServices();
        }
        catch (Exception ex)
        {
            log.Fatal(ex, "Failed to build service collection");
            Environment.Exit(1);
            return;
        }

        var scope = services.CreateScope();
        RunAsync(scope.ServiceProvider).Wait();
    }
    private static async Task PerformDatabaseInit(IServiceProvider services)
    {
        // FunctionalGlue.ApplyDatabaseMigrations(services);

        // var log = LogManager.GetLogger("ShardedBot.Init");
        // var db = services.GetRequiredService<XeniaDbContext>();
        // await using var sysSetTrans = await db.Database.BeginTransactionAsync();
        // try
        // {
        //     var _ = db.GetSystemSettings();
        //     await db.SaveChangesAsync();
        //     await sysSetTrans.CommitAsync();
        // }
        // catch (Exception ex)
        // {
        //     await sysSetTrans.RollbackAsync();
        //     log.Error(ex, "Failed to save system settings!");
        // }
    }
    private static async Task RunAsync(IServiceProvider services)
    {
        var log = LogManager.GetLogger("ShardedBot.RunAsync");
        log.Info("Warming up database");

        FunctionalGlue.ApplyDatabaseMigrations(services);
        // await PerformDatabaseInit(services);

        var discord = services.GetRequiredService<DiscordShardedClient>();
        discord.ShardReady += client =>
        {
            log.Info($"Shard Ready ({client.ShardId})");
            var innerLog = LogManager.GetLogger("Discord.DiscordShardedClient");
            new Thread(() =>
            {
                innerLog.Info("Sending \"Ready\" notification.");
                FunctionalGlue.NotifyDiscordReady(XeniaServiceList, services);
            }).Start();
            return Task.CompletedTask;
        };
        AllBaseServices(services, async (s) =>
        {
            await s.ActivateAsync();
        });
        var cfg = XeniaConfig.Get();
        log.Info("Connecting to Discord...");
        await discord.LoginAsync(TokenType.Bot, cfg.Discord.Token);
        log.Info("Connected!");
        // TODO implement inline health service (copy from XeniaBotReborn)
        // try
        // {
        //     InlineHealthService.RunThread(services);
        // }
        // catch (Exception ex)
        // {
        //     log.Error(ex, "Failed to run thread for Health Service");
        // }
        await discord.StartAsync();
        await Task.Delay(-1);
    }
    private static IServiceProvider GetServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(XeniaConfig.Get());
        services.WithDatabaseServices(new()
        {
            DatabaseDeveloperPageExceptionFilter = IsDevelopment,
            EnableSensitiveDataLogging = IsDevelopment
        });
        services.WithCacheServices();
        services.WithMongoDb();
        services.WithDiscord(new()
        {
            UseWebsocket = true,
            UseShards = true,
            UseInteractions = true,
            InteractionConfig = new InteractionServiceConfig()
            {
                UseCompiledLambda = true,
                AutoServiceScopes = false
            },
            SocketConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            },
            AutoLogin = false
        }, (interaction, p) =>
        {
            XeniaDiscordSharedInteractions.RegisterModules(interaction, p).Wait();
        });

        XeniaDiscordCommon.RegisterServices(services);
        XeniaBotShared.RegisterServices(services);

        XeniaServiceList = FunctionalGlue.FindServicesThatExtend<IXeniaService>(services).Select(e => e.ServiceType).ToList();
        return services.BuildServiceProvider();
    }
    private static void AllBaseServices(IServiceProvider services, Func<IXeniaService, Task> func)
    {
        var taskList = new List<Task>();
        var ins = new List<IXeniaService>();
        foreach (var service in XeniaServiceList)
        {
            foreach (var item in services.GetServices(service) ?? [])
            {
                if (item != null && item.GetType().IsAssignableTo(typeof(IXeniaService)))
                {
                    ins.Add((IXeniaService)item);
                }
            }
        }
        foreach (var item in ins)
        {
            taskList.Add(func(item));
        }
        Task.WaitAll(taskList.ToArray());
    }
    private static IReadOnlyList<Type> XeniaServiceList { get; set; } = [];
}