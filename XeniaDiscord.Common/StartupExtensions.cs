using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using EFCoreSecondLevelCacheInterceptor;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.ComponentModel;
using XeniaBot.Shared;
using XeniaBot.Shared.Config;
using XeniaDiscord.Data;

namespace XeniaDiscord.Common;

public static class StartupExtensions
{
    public static void WithMongoDb(this IServiceCollection services)
    {
        var cfg = XeniaConfig.Get();
        var connectionSettings = MongoClientSettings.FromConnectionString(cfg.MongoDb.ConnectionUrl);
        connectionSettings.AllowInsecureTls = true;
        connectionSettings.MaxConnectionPoolSize = 500;
        connectionSettings.WaitQueueSize = 2000;
        var client = new MongoClient(connectionSettings);
        client.StartSession();
        var database = client.GetDatabase(cfg.MongoDb.Database);
        services.AddSingleton<IMongoDatabase>(database);
    }

    public static void WithDiscord(
        this IServiceCollection services,
        DiscordOptions opts,
        Action<ForeverInteractionService, IServiceProvider>? registerInteractionServicesCallback = null)
    {
        if (opts.UseInteractions && registerInteractionServicesCallback == null)
        {
            throw new ArgumentNullException(nameof(registerInteractionServicesCallback), $"Argument is required when {nameof(opts.UseInteractions)} is enabled");
        }
        var cfg = XeniaConfig.Get();

        IRestClientProvider client;
        if (opts.UseWebsocket)
        {
            if (opts.UseShards)
            {
                client = new ForeverDiscordShardedClient();
                if (opts.SocketConfig != null && opts.ShardIds == null)
                    client = new ForeverDiscordShardedClient(opts.SocketConfig);
                else if (opts.SocketConfig != null && opts.ShardIds != null)
                    client = new ForeverDiscordShardedClient(opts.ShardIds, opts.SocketConfig);

                if (opts.AutoLogin)
                    ((ForeverDiscordShardedClient)client).LoginAsync(TokenType.Bot, cfg.Discord.Token).Wait();

                services
                    .AddScoped(typeof(DiscordShardedClient), _ => c)
                    .AddScoped(typeof(IRestClientProvider), _ => c)
                    .AddScoped(typeof(IDiscordClient), _ => c)
                    .AddSingleton(typeof(DiscordShardedClient), c)
                    .AddSingleton(typeof(IRestClientProvider), c)
                    .AddSingleton(typeof(IDiscordClient), c);
            }
            else
            {
                var c = opts.SocketConfig == null
                    ? new ForeverDiscordSocketClient()
                    : new ForeverDiscordSocketClient(opts.SocketConfig);
                client = c;
                if (opts.AutoLogin) c.LoginAsync(TokenType.Bot, cfg.Discord.Token).Wait();

                services
                    .AddScoped(typeof(DiscordSocketClient), _ => c)
                    .AddScoped(typeof(IRestClientProvider), _ => c)
                    .AddScoped(typeof(IDiscordClient), _ => c)
                    .AddSingleton(typeof(DiscordSocketClient), c)
                    .AddSingleton(typeof(IRestClientProvider), c)
                    .AddSingleton(typeof(IDiscordClient), c);
            }
        }
        else
        {
            var c = opts.RestConfig == null
                ? new ForeverDiscordRestClient()
                : new ForeverDiscordRestClient(opts.RestConfig);
            client = c;
            if (opts.AutoLogin) c.LoginAsync(TokenType.Bot, cfg.Discord.Token).Wait();

            services
                .AddScoped(typeof(DiscordRestClient), _ => c)
                .AddScoped(typeof(IRestClientProvider), _ => c)
                .AddScoped(typeof(IDiscordClient), _ => c)
                .AddSingleton(typeof(DiscordRestClient), c)
                .AddSingleton(typeof(IRestClientProvider), c)
                .AddSingleton(typeof(IDiscordClient), c);
        }

        if (opts.UseInteractions)
        {
            if (registerInteractionServicesCallback == null)
            {
                throw new ArgumentNullException(nameof(registerInteractionServicesCallback), $"Argument is required when {nameof(opts.UseInteractions)} is enabled");
            }

            var interaction = new ForeverInteractionService(
                client,
                opts.InteractionConfig ?? new InteractionServiceConfig()
                {
                    UseCompiledLambda = true,
                    AutoServiceScopes = true
                });

            var d = false;

            services.AddScoped(typeof(InteractionService), p =>
            {
                if (d) return interaction;
                lock (interaction)
                {
                    if (d) return interaction;
                    registerInteractionServicesCallback(interaction, p);
                    interaction.RegisterCommandsGloballyAsync().Wait();
                    d = true;
                }
                return interaction;
            });
        }
    }

    public class DiscordOptions
    {
        /// <summary>
        /// When <see langword="true"/>, <see cref="ForeverInteractionService"/> will be added.
        /// </summary>
        public bool UseInteractions { get; set; }

        /// <summary>
        /// When <see langword="true"/>, <see cref="ForeverDiscordSocketClient"/> will be used.
        /// Otherwise, <see cref="ForeverDiscordRestClient"/> will be used.
        /// </summary>
        public bool UseWebsocket { get; set; }

        /// <summary>
        /// When <see cref="UseWebsocket"/> is <see langword="true"/>,
        /// then <see cref="ForeverDiscordShardedClient"/> will be used instead of <see cref="ForeverDiscordSocketClient"/>.
        /// </summary>
        public bool UseShards { get; set; }

        public bool AutoLogin { get; set; } = true;

        public InteractionServiceConfig? InteractionConfig { get; set; }
        public DiscordRestConfig? RestConfig { get; set; }
        public DiscordSocketConfig? SocketConfig { get; set; }
        public int[]? ShardIds { get; set; }
    }

    public static void WithDatabaseServices(this IServiceCollection services, DatabaseServicesOptions options)
    {
        // Add services to the container.
        services.AddDbContextPool<ApplicationDbContext>(
            o =>
            {
                var cfg = XeniaConfig.Get();
                var connectionString = cfg.Database.ToConnectionString();
                o.UseNpgsql(connectionString);

                if (options.EnableSensitiveDataLogging)
                {
                    o.EnableSensitiveDataLogging();
                }
            });
        // TODO for asp.net
        // if (options.DatabaseDeveloperPageExceptionFilter)
        // {
        //     services.AddDatabaseDeveloperPageExceptionFilter();
        // }
    }

    public class DatabaseServicesOptions
    {
        /// <summary>
        /// Enabled in development mode.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableSensitiveDataLogging { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(false)]
        public bool DatabaseDeveloperPageExceptionFilter { get; set; } = false;
    }

    public static void WithCacheServices(this IServiceCollection services)
    {
        var logger = NLog.LogManager.GetLogger(nameof(StartupExtensions) + "." + nameof(WithCacheServices));
        var cfg = XeniaConfig.Get();

        if (cfg.Cache.InMemory == null && cfg.Cache.Redis == null)
        {
            cfg.Cache.InMemory ??= new();
        }
        var providerName = cfg.Cache.Redis != null ? "Redis" : "InMemory";

        services.AddEFSecondLevelCache(options =>
                    options.UseEasyCachingCoreProvider(providerName, isHybridCache: false)
                        .ConfigureLogging(true)
                        .UseCacheKeyPrefix(cfg.Cache.CachePrefix)
                        // Fallback on db if the caching provider fails (for example, if Redis is down).
                        .UseDbCallsIfCachingProviderIsDown(TimeSpan.FromMinutes(1))
            );

        services.AddEasyCaching(options =>
        {
            cfg = XeniaConfig.Get();
            var enableRedis = cfg.Cache.Redis?.Enable ?? false;
            if (enableRedis)
            {
                enableRedis = cfg.Cache.Redis!.DbConfig.Endpoints.Count >= 1;
                if (!enableRedis)
                {
                    logger.Warn("Disabling Redis Cache since no endpoints are defined.");
                }
            }
            if (enableRedis)
            {
                var redisConfig = cfg.Cache.Redis!;
                options.UseRedis(config =>
                {
                    config.DBConfig = new()
                    {
                        Database = redisConfig.DbConfig.Database,
                        AsyncTimeout = redisConfig.DbConfig.AsyncTimeout,
                        SyncTimeout = redisConfig.DbConfig.SyncTimeout,
                        KeyPrefix = cfg.Cache.CachePrefix,

                        Username = string.IsNullOrEmpty(redisConfig.DbConfig.Username) ? "" : redisConfig.DbConfig.Username,
                        Password = string.IsNullOrEmpty(redisConfig.DbConfig.Password) ? "" : redisConfig.DbConfig.Password,
                        IsSsl = redisConfig.DbConfig.SslEnabled,
                        SslHost = redisConfig.DbConfig.SslHost,
                        ConnectionTimeout = redisConfig.DbConfig.ConnectionTimeout,
                        AllowAdmin = redisConfig.DbConfig.AllowAdmin,
                        AbortOnConnectFail = redisConfig.DbConfig.AbortOnConnectFail,
                    };
                    config.DBConfig.Endpoints.Clear();
                    foreach (var endpoint in redisConfig.DbConfig.Endpoints)
                    {
                        config.DBConfig.Endpoints.Add(new(endpoint.Host, endpoint.Port));
                    }
                    config.EnableLogging = redisConfig.EnableLogging;
                    config.SerializerName = "Pack";

                }, "Redis")
                .WithMessagePack(so =>
                {
                    so.EnableCustomResolver = true;
                    var formatters = new IMessagePackFormatter[]
                    {
                        DBNullFormatter.Instance, // This is necessary for the null values
                    };
                    var formatterResolvers = new IFormatterResolver[]
                    {
                        NativeDateTimeResolver.Instance,
                        ContractlessStandardResolver.Instance,
                        StandardResolverAllowPrivate.Instance,
                    };
                    so.CustomResolvers = CompositeResolver.Create(formatters, formatterResolvers);
                }, "Pack");
            }
            else
            {
                var memoryConfig = cfg.Cache.InMemory ?? new();
                options.UseInMemory(config =>
                {
                    config.DBConfig = new EasyCaching.InMemory.InMemoryCachingOptions()
                    {
                        ExpirationScanFrequency = memoryConfig.DbConfig.ExpirationScanFrequency,
                        SizeLimit = memoryConfig.DbConfig.SizeLimit,
                        EnableReadDeepClone = memoryConfig.DbConfig.EnableReadDeepClone,
                        EnableWriteDeepClone = memoryConfig.DbConfig.EnableWriteDeepClone
                    };

                    config.MaxRdSecond = memoryConfig.MaxRandomSeconds;
                    config.EnableLogging = memoryConfig.EnableLogging;
                    config.LockMs = memoryConfig.LockMilliseconds;
                    config.SleepMs = memoryConfig.SleepMilliseconds;
                }, "InMemory");
            }
        });
    }
}
