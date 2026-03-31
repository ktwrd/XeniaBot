using CronNET;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace XeniaBot.Shared.Services;

/// <summary>
/// A Monolithic class that controls all the important aspects of getting the services and required controllers ready for a working Xenia project.
/// </summary>
public class CoreContext
{
    private static readonly Logger Log = LogManager.GetLogger("Xenia.CoreContext");
    public static CoreContext? Instance { get; private set; }
    public CoreContext(ProgramDetails details)
    {
        if (Instance != null)
        {
            throw new InvalidOperationException("An instance of CoreContext exists already.");
        }

        Details = details;
        RegisteredBaseControllers = [];
        Instance = this;
        Config = new ConfigService(Details);
        Discord = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.All,
            UseInteractionSnowflakeDate = false,
            AlwaysDownloadUsers = true,
            ShardId = Config.Data.ShardId
        });
    }

    public async Task MainAsync(string[] args, CoreContextBeforeServiceBuildDelegate beforeServiceBuild)
    {
        if (StartTimestamp == 0)
            StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var objectSerializer = new ObjectSerializer(type
            => ObjectSerializer.DefaultAllowedTypes(type)
            || type.FullName?.StartsWith("XeniaBot") == true
            || type.FullName?.StartsWith("XeniaDiscord") == true);
        BsonSerializer.RegisterSerializer(objectSerializer);

        InitMongoClient();
        InitServices(beforeServiceBuild);

        var discordService = Services.GetRequiredService<DiscordService>();
        discordService.Ready += DiscordServiceOnReady;
        await discordService.Run();
        if (AlternativeMain != null)
        {
            await AlternativeMain(args);
        }

        if (Config.Data.Health.Enable
            && Details.Platform == XeniaPlatform.Bot)
        {
            Services.GetRequiredService<HealthServer>().Run();
        }
    }
    private void DiscordServiceOnReady(DiscordService service)
    {
        new Thread(DiscordServiceOnReadyThreadHandler)
        {
            Name = $"{nameof(CoreContext)}.{nameof(DiscordServiceOnReadyThreadHandler)}"
        }.Start();
    }
    private void DiscordServiceOnReadyThreadHandler()
    {
        try
        {
            RunServiceReady();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to run " + nameof(RunServiceReady));
        }
        Task.Delay(2000).GetAwaiter().GetResult();
        try
        {
            RunServiceDelayedReady();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to run " + nameof(RunServiceDelayedReady));
        }
    }
    /// <summary>
    /// When not null, it is called after <see cref="DiscordService.Run()"/> in <see cref="MainAsync"/>.
    /// </summary>
    public CoreContextAlternativeMainDelegate? AlternativeMain { get; set; }
    public CoreContextRegisterInteractionModulesDelegate RegisterModules { get; set; } = DefaultRegisterModules;
    public CoreContextGetDeveloperModulesDelegate? RegisterDeveloperModules { get; set; }
    private static async Task DefaultRegisterModules(InteractionService interactions, IServiceProvider services)
    {
        foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
        {
            await interactions.AddModulesAsync(item, services);
        }
    }
    public ProgramDetails Details { get; private set; }
    public ConfigService Config { get; private set; }
    public DiscordSocketClient Discord { get; private set; }
    /// <summary>
    /// Created after <see cref="InjectServices"/> is called in <see cref="MainAsync"/>
    /// </summary>
    public ServiceProvider Services { get; private set; }
    public T GetRequiredService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    /// <summary>
    /// UTC of <see cref="DateTimeOffset.ToUnixTimeSeconds()"/>
    /// </summary>
    public long StartTimestamp { get; set; } = 0;

    public static readonly JsonSerializerOptions SerializerOptions
        = new()
        {
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            IncludeFields = true,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };
    public MongoClient MongoDB { get; set; }

    /// <summary>
    /// Initialize MongoDB Client (<see cref="MongoDB"/>)
    /// </summary>
    public void InitMongoClient()
    {
        try
        {
            Log.Debug("Connecting to MongoDB");
            var connectionSettings = MongoClientSettings.FromConnectionString(Config.Data.MongoDB.ConnectionUrl);
            connectionSettings.AllowInsecureTls = true;
            connectionSettings.MaxConnectionPoolSize = 500;
            connectionSettings.WaitQueueSize = 2000;
            MongoDB = new MongoClient(connectionSettings);
            MongoDB.StartSession();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to connect to MongoDB Server");
            OnQuit(1);
        }
    }
    public IMongoDatabase GetDatabase()
    {
        return MongoDB.GetDatabase(Config.Data.MongoDB.DatabaseName);
    }

    #region Services
    public void InitServices(CoreContextBeforeServiceBuildDelegate beforeServiceBuild)
    {
        InjectServices(new ServiceCollection(), beforeServiceBuild);
    }
    /// <summary>
    /// Initialize all service-related stuff. <see cref="DiscordService"/> is also created here and added as a singleton to <see cref="Services"/>
    /// </summary>
    public void InjectServices(ServiceCollection services, CoreContextBeforeServiceBuildDelegate beforeBuild)
    {
        InjectServicesDirect(services);
        beforeBuild(services).GetAwaiter().GetResult();

        RegisteredBaseControllers = [..services.Where(item
            => item.ServiceType.IsAssignableTo(typeof(BaseService))
            && !RegisteredBaseControllers.Contains(item.ServiceType)).Select(item => item.ServiceType)];
        Services = services.BuildServiceProvider();
        RunServiceInit();
    }
    public void InjectServicesDirect(IServiceCollection services)
    {
        services
            .AddSingleton(this)
            .AddSingleton(Details)
            .AddSingleton<CronDaemon>()
            .AddSingleton(Config)
            .AddSingleton(Config.Data)
            .AddSingleton(Discord)
            .AddSingleton<IDiscordClient>(Discord)
            .AddSingleton<HealthServer>();

        var mongoDb = GetDatabase();
        if (mongoDb == null)
        {
            Log.Error($"FATAL ERROR!!! CoreContext.GetDatabase() returned null! (failed to get mongodb)");
            OnQuit(1);
        }

        var s = new InteractionService(Discord);

        services.AddSingleton(mongoDb)
            .AddSingleton<DiscordService>()
            .AddSingleton<CommandService>()
            .AddSingleton(s)
            .AddSingleton<InteractionHandler>();
    }
    private List<Type> RegisteredBaseControllers { get; set; }

    private void RunServiceInit()
    {
        AllBaseServices(async (item) =>
        {
            await item.InitializeAsync();
        });
        Log.Info("Done");
    }

    private void RunServiceReady()
    {
        AllBaseServices(async (item) =>
        {
            await item.OnReady();
        });
        Log.Info("Done - Bot is online!");
    }

    public void RunServiceDelayedReady()
    {
        AllBaseServices(async (item) =>
        {
            await item.OnReadyDelay();
        });
        Log.Info("Done");
    }
    /// <summary>
    /// For every registered class that extends <see cref="BaseService"/>, call <paramref name="func"/> with the argument as the target controller.
    /// </summary>
    public void AllBaseServices(Func<BaseService, Task> func)
    {
        var targetServices = new List<BaseService>();
        foreach (var service in RegisteredBaseControllers.Where(e => typeof(BaseService).IsAssignableFrom(e)))
        {
            var svc = Services.GetServices(service);
            foreach (var item in svc.Where(e => e != null).Cast<object>())
            {
                if (item is BaseService svcItem)
                {
                    targetServices.Add(svcItem);
                }
            }
        }
        Task.WhenAll(targetServices
            .OrderBy(v => v.Priority)
            .ThenBy(v => v.GetType().AssemblyQualifiedName)
            .Select(ProcessItem))
            .GetAwaiter().GetResult();
        async Task ProcessItem(BaseService svc)
        {
            await func(svc);
        }
    }
    #endregion

    [DoesNotReturn]
    public void OnQuit(int code)
    {
        BeforeQuit();
        Environment.Exit(code);
    }

    private void BeforeQuit()
    {
        Config.Write(Config.Data);
    }
}

public delegate Task CoreContextRegisterInteractionModulesDelegate(InteractionService interactions, IServiceProvider services);
public delegate Task<Discord.Interactions.ModuleInfo[]> CoreContextGetDeveloperModulesDelegate(InteractionService interactions, IServiceProvider services);
public delegate Task CoreContextBeforeServiceBuildDelegate(IServiceCollection services);
// Func<string[], Task>
public delegate Task CoreContextAlternativeMainDelegate(string[] args);