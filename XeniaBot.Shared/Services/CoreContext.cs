﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CronNET;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace XeniaBot.Shared.Services;

/// <summary>
/// A Monolithic class that controls all the important aspects of getting the services and required controllers ready for a working Xenia project.
/// </summary>
public class CoreContext
{
    public static CoreContext? Instance { get; private set; }
    public CoreContext(ProgramDetails details)
    {
        if (Instance != null)
        {
            throw new Exception("An instance of CoreContext exists already.");
        }

        Details = details;

        RegisteredBaseControllers = new List<Type>();

        Instance = this;
    }

    public async Task MainAsync(string[] args, Func<ServiceCollection, Task> beforeServiceBuild)
    {
        if (StartTimestamp == 0)
            StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("XeniaBot"));
        BsonSerializer.RegisterSerializer(objectSerializer);

        Config = new ConfigService(Details);
        Discord = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.All,
            UseInteractionSnowflakeDate = false,
            ShardId = Config.Data.ShardId
        });
        InitMongoClient();
        InitServices(beforeServiceBuild);

        var discordController = Services.GetRequiredService<DiscordService>();
        discordController.Ready += (c) =>
        {
            new Thread((ThreadStart)delegate
            {
                RunServiceReady();
                Task.Delay(2000).Wait();
                RunServiceDelayedReady();
            }).Start();
        };
        await discordController.Run();
        if (AlternativeMain != null)
        {
            await AlternativeMain(args);
        }

        if (Config.Data.Health.Enable && Details.Platform == XeniaPlatform.Bot)
        {
            new HealthServer(this, Details.PlatformTag).Run(Config.Data.Health.Port);
        }
    }
    /// <summary>
    /// When not null, it is called after <see cref="DiscordService.Run()"/> in <see cref="MainAsync"/>.
    /// </summary>
    public Func<string[], Task>? AlternativeMain { get; set; }
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

    public static JsonSerializerOptions SerializerOptions =>
        new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
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
            Log.Error($"Failed to connect to MongoDB Server\n{ex}");
            OnQuit(1);
        }
    }
    public IMongoDatabase GetDatabase()
    {
        return MongoDB.GetDatabase(Config.Data.MongoDB.DatabaseName);
    }

    #region Services
    public void InitServices(Func<ServiceCollection, Task> beforeServiceBuild)
    {
        InjectServices(new ServiceCollection(), beforeServiceBuild);
    }
    /// <summary>
    /// Initialize all service-related stuff. <see cref="DiscordService"/> is also created here and added as a singleton to <see cref="Services"/>
    /// </summary>
    public void InjectServices(ServiceCollection services, Func<ServiceCollection, Task> beforeBuild)
    {
        services
            .AddSingleton(this)
            .AddSingleton(Details)
            .AddSingleton<CronDaemon>()
            .AddSingleton(Config)
            .AddSingleton(Config.Data)
            .AddSingleton(Discord);

        var mongoDb = GetDatabase();
        if (mongoDb == null)
        {
            Log.Error($"FATAL ERROR!!! CoreContext.GetDatabase() returned null!");
            OnQuit(1);
        }

        var s = new InteractionService(Discord);

        services.AddSingleton(mongoDb)
            .AddSingleton<DiscordService>()
            .AddSingleton<CommandService>()
            .AddSingleton(s)
            .AddSingleton<CommandHandler>()
            .AddSingleton<InteractionHandler>();

        beforeBuild(services).Wait();

        RegisteredBaseControllers = new List<Type>();
        foreach (var item in services)
        {
            if (item.ServiceType.IsAssignableTo(typeof(BaseService)) &&
                !RegisteredBaseControllers.Contains(item.ServiceType))
            {
                RegisteredBaseControllers.Add(item.ServiceType);
            }
        }

        Services = services.BuildServiceProvider();
        RunServiceInit();
    }
    private List<Type> RegisteredBaseControllers { get; set; }

    private void RunServiceInit()
    {
        AllBaseServices((item) =>
        {
            item.InitializeAsync().Wait();
            return Task.CompletedTask;
        });
    }

    private void RunServiceReady()
    {
        AllBaseServices((item) =>
        {
            item.OnReady().Wait();
            return Task.CompletedTask;
        });
    }

    public void RunServiceDelayedReady()
    {
        AllBaseServices((item) =>
        {
            item.OnReadyDelay();
            return Task.CompletedTask;
        });
    }
    /// <summary>
    /// For every registered class that extends <see cref="BaseService"/>, call <paramref name="func"/> with the argument as the target controller.
    /// </summary>
    public void AllBaseServices(Func<BaseService, Task> func)
    {
        var taskList = new List<Task>();
        var ins = new List<BaseService>();
        foreach (var service in RegisteredBaseControllers)
        {
            var svc = Services.GetServices(service);
            foreach (var item in svc)
            {
                if (item != null && item.GetType().IsAssignableTo(typeof(BaseService)))
                {
                    ins.Add((BaseService)item);
                }
            }
        }
        ins = ins.OrderBy(v => v.Priority)
            .ThenBy(v => v.GetType().AssemblyQualifiedName).ToList();
        foreach (var item in ins)
        {
            taskList.Add(new Task(delegate
            {
                func((BaseService)item).Wait();
            }));
        }
        foreach (var i in taskList)
            i.Start();
        Task.WaitAll(taskList.ToArray());
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