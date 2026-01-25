using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using XeniaBot.Shared;
using XeniaBot.Shared.Config;
using XeniaDiscord.Common;

namespace XeniaBot.WebPanel;

public static class Program
{
    /// <summary>
    /// Created after <see cref="CreateServiceProvider"/> is called in <see cref="MainAsync(string[])"/>
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
    {
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve
    };
    /// <summary>
    /// UTC of <see cref="DateTimeOffset.ToUnixTimeSeconds()"/>
    /// </summary>
    public static long StartTimestamp { get; private set; }
    public static Version? Version
    {
        get
        {
            var v = Assembly.GetAssembly(typeof(Program))?.GetName().Version;
            if (v == null)
                return null;

            return new Version(v.Major, v.Minor, v.Build, (v.Revision * 2) / 60);
        }
    }

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
        StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

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
    private static async Task RunAsync(IServiceProvider services)
    {
        var log = LogManager.GetLogger("WebPanel.RunAsync");
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
        await Main_AspNet(Environment.GetCommandLineArgs());
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
            UseInteractions = false,
            SocketConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            },
            AutoLogin = false
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

    public static async Task Main_AspNet(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddMvc().AddRazorRuntimeCompilation();
        builder.Services.AddControllersWithViews();
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/signin";
                options.LogoutPath = "/signout";
            })
            .AddDiscord(options =>
            {
                options.ClientId = XeniaConfig.Instance!.Discord.OAuthId!;
                options.ClientSecret = XeniaConfig.Instance!.Discord.OAuthSecret!;

                options.ClaimActions.MapCustomJson("urn:discord:avatar:url", user =>
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "https://cdn.discordapp.com/avatars/{0}/{1}.{2}",
                        user.GetString("id"),
                        user.GetString("avatar"),
                        (user.GetString("avatar")?.StartsWith("a_") ?? false) ? "gif" : "png"));
            });
        builder.Services.AddServerSideBlazor();
        builder.WebHost.UseSentry(FeatureFlags.SentryDsn);
        var app = builder.Build();
        app.UseStaticFiles();
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
            app.UseSentryTracing();
        }

        if (app.Environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
        }

        // Required to serve files with no extension in the .well-known folder
        var options = new StaticFileOptions()
        {
            ServeUnknownFileTypes = true,
        };

        app.UseStaticFiles(options);

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
        app.MapBlazorHub();
        /*var options = new StaticFileOptions()
        {
            ServeUnknownFileTypes = true,
        };

        app.UseHttpsRedirection();
        app.UseStaticFiles(options);

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });*/

        await app.RunAsync();
    }
}