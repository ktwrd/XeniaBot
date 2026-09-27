using System.ComponentModel;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;

namespace XeniaDiscord.Hosting;

public static class HostExtensions
{
    public static IHostBuilder UseXeniaCore(this IHostBuilder builder)
    {
        return builder.ConfigureServices(services =>
        {
            services.AddXeniaCore(new());
        });
    }
    
    public static IServiceCollection AddXeniaCore(
        this IServiceCollection services,
        XeniaCoreOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<DatabaseServicesOptions>(static s => s.GetRequiredService<XeniaCoreOptions>().Database);

        services.AddSingleton<ConfigService>(static s => new ConfigService(s));
        services.AddSingleton<ConfigData>(static s => s.GetRequiredService<ConfigService>().Data);
        
        services.AddXeniaDatabase(options.Database);
        services.AddXeniaMongo();
        
        // discord
        services.AddXeniaDiscord();
        services
            .AddSingleton<DiscordService>()
            .AddSingleton<IDiscordClient>(static svc => svc.GetRequiredService<DiscordShardedClient>())
            .AddSingleton<BaseSocketClient>(static svc => svc.GetRequiredService<DiscordShardedClient>())
            .AddSingleton<IRestClientProvider>(static svc => svc.GetRequiredService<DiscordShardedClient>())
            .AddSingleton<DiscordRestClient>(static svc => svc.GetRequiredService<IRestClientProvider>().RestClient)
            .AddSingleton<DiscordClientProxy>()
            .AddSingleton<IDiscordClientProxy>(static svc => svc.GetRequiredService<DiscordClientProxy>());

        if (options.UseCommands)
        {
            services
                .AddSingleton(static svc => new InteractionService(
                    svc.GetRequiredService<IRestClientProvider>(),
                    new InteractionServiceConfig()
                    {
                        UseCompiledLambda = true
                    }))
                .AddSingleton<CommandService>()
                .AddSingleton<InteractionHandler>();
        }
        
        services.AddHostedService<XeniaHostedService>();
        
        XeniaDiscordData.RegisterServices(services, true);
        AttributeHelper.InjectControllerAttributes(typeof(XeniaDiscordData).Assembly, services);
        AttributeHelper.InjectControllerAttributes(typeof(CoreContext).Assembly, services); // XeniaBot.Shared
        return services;
    }

    public class XeniaCoreOptions
    {
        public bool UseCommands { get; set; }

        public DatabaseServicesOptions Database { get; set; } = new();
    }

    public static void AddXeniaDatabase(this IServiceCollection services, DatabaseServicesOptions options)
    {
        services.AddSingleton(options);
        services.AddDbContextPool<XeniaDbContext>(ConfigureDbContextOptionsBuilder);
        services.AddPooledDbContextFactory<XeniaDbContext>(ConfigureDbContextOptionsBuilder);
        //services.AddDbContextPool<XeniaDbContext>(o => ConfigureDbContextOptionsBuilder(o, options));
        //services.AddPooledDbContextFactory<XeniaDbContext>(o => ConfigureDbContextOptionsBuilder(o, options));
        // TODO for asp.net
        if (options.DatabaseDeveloperPageExceptionFilter)
        {
            services.AddDatabaseDeveloperPageExceptionFilter();
        }
    }
    
    private static void ConfigureDbContextOptionsBuilder(
        IServiceProvider services,
        DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = ConfigService.Instance.Data.Postgres.ToConnectionString();
        optionsBuilder.UseNpgsql(connectionString);
        if (services.GetRequiredService<DatabaseServicesOptions>().EnableSensitiveDataLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }
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

    public static void AddXeniaMongo(this IServiceCollection services)
    {
        services.AddSingleton<MongoClientSettings>(static s =>
        {
            var settings = MongoClientSettings.FromConnectionString(s.GetRequiredService<ConfigData>().MongoDB.ConnectionUrl);
            settings.AllowInsecureTls = true;
            settings.MaxConnectionPoolSize = 500;
            settings.WaitQueueSize = 2000;
            return settings;
        });
        services.AddSingleton<MongoClient>(static s =>
        {
            var settings = s.GetRequiredService<MongoClientSettings>();
            var db = new MongoClient(settings);
            db.StartSession();
            return db;
        });
        services.AddSingleton<IMongoDatabase>(static s =>
        {
            var name = s.GetRequiredService<ConfigData>().MongoDB.DatabaseName;
            var client = s.GetRequiredService<MongoClient>();
            if (client.ListDatabaseNames().ToList().All(e => e != name)) throw new InvalidOperationException("Could not find MongoDB database: " + name);
            return client.GetDatabase(name);
        });
    }

    public static void AddXeniaDiscord(this IServiceCollection services)
    {
        services.AddSingleton<DiscordSocketConfig>(static s =>
        {
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
                UseInteractionSnowflakeDate = false,
                AlwaysDownloadUsers = true,
            };
            if (s.GetRequiredService<ProgramDetails>().Platform == XeniaPlatform.Bot)
            {
                config.GatewayIntents |= GatewayIntents.MessageContent;
            }

            return config;
        });
        services.AddSingleton<DiscordShardedClient>(static s =>
        {
            return new DiscordShardedClient(s.GetRequiredService<DiscordSocketConfig>());
        });
    }
}