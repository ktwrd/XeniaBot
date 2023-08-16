using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using XeniaBot.Data;
using XeniaBot.Data.Controllers;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Shared;
public static class Program
{
    
    #region Fields
    public static ConfigManager ConfigManager = null;
    public static ConfigData ConfigData = null;
    public static HttpClient HttpClient = null;
    public static MongoClient MongoClient = null;
    private static DiscordController _discordController;
    /// <summary>
    /// Created after <see cref="CreateServiceProvider"/> is called in <see cref="MainAsync(string[])"/>
    /// </summary>
    public static ServiceProvider Services = null;
    public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
    {
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = true,
        WriteIndented = true
    };
    /// <summary>
    /// UTC of <see cref="DateTimeOffset.ToUnixTimeSeconds()"/>
    /// </summary>
    public static long StartTimestamp { get; private set; }
    public const string MongoDatabaseName = "xenia_discord";
    #endregion
    public static IMongoDatabase? GetMongoDatabase()
    {
        return MongoClient.GetDatabase(MongoDatabaseName);
    }
    public static ProgramDetails Details => new ProgramDetails()
    {
        VersionRaw = typeof(Program).Assembly.GetName()?.Version,
        Debug = 
#if DEBUG
true
#else
false
#endif
    };
    public static void Main(string[] args)
    {
        /*var fo = File.Open("log.txt", FileMode.OpenOrCreate);
        var efo = File.Open("log.error.txt", FileMode.OpenOrCreate);
        var so = new StreamWriter(fo);
        var eso = new StreamWriter(efo);
        Console.SetOut(so);
        Console.SetError(eso);*/
        
        StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        MainInit();
        MainInit_ValidateMongo();
        MainAsync(args).Wait();
    }

    public static void MainInit()
    {
        ConfigManager = new ConfigManager();
        ConfigData = ConfigManager.Read();
        HttpClient = new HttpClient();
    }

    public static void MainInit_ValidateMongo()
    {
        try
        {
            Log.Debug("Connecting to MongoDB");
            var connectionSettings = MongoClientSettings.FromConnectionString(ConfigData.MongoDBServer);
            connectionSettings.AllowInsecureTls = true;
            MongoClient = new MongoClient(connectionSettings);
            MongoClient.StartSession();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to connect to MongoDB Server\n{ex}");
            Environment.Exit(1);
        }
    }

    public static async Task MainAsync(string[] args)
    {
        CreateServiceProvider();
        Log.Debug("Connecting to Discord");
        _discordController = Services.GetRequiredService<DiscordController>();
        _discordController.Ready += (c) =>
        {
            RunServicesReadyFunc();
        };
        await _discordController.Run();
        await Main_AspNet(args);

        await Task.Delay(-1);
    }

    public static Version? Version => typeof(Program).Assembly.GetName().Version;
    public static string VersionFull => $"v{Version?.Major}.{Version?.Minor} ({VersionDate})";
    public static DateTime VersionDate
    {
        get
        {
            DateTime buildDate = new DateTime(2000, 1, 1)
                .AddDays(Version?.Build ?? 0)
                .AddSeconds((Version?.Revision ?? 0) * 2);
            return buildDate;
        }
    }
    #region Service Provider
    /// <summary>
    /// Initialize all service-related stuff. <see cref="DiscordController"/> is also created here and added as a singleton to <see cref="Services"/>
    /// </summary>
    private static void CreateServiceProvider()
    {
        Log.Debug("Initializing Services");
        var dsc = new DiscordSocketClient(DiscordController.GetSocketClientConfig());
        var services = new ServiceCollection();

        var details = new ProgramDetails()
        {
            StartTimestamp = StartTimestamp,
            VersionRaw = typeof(Program).Assembly.GetName().Version,
            Platform = XeniaPlatform.WebPanel
        };

        services.AddSingleton(details)
            .AddSingleton(ConfigManager)
            .AddSingleton(ConfigData)
            .AddSingleton(dsc)
            .AddSingleton(GetMongoDatabase())
            .AddSingleton<DiscordController>()
            .AddSingleton<PrometheusController>();

        AttributeHelper.InjectControllerAttributes(typeof(Program).Assembly, services);
        AttributeHelper.InjectControllerAttributes(typeof(ServerLogConfigController).Assembly, services);
        _serviceClassExtendsBaseController = new List<Type>();

        foreach (var item in services)
        {
            if (item.ServiceType.IsAssignableTo(typeof(BaseController)) && !_serviceClassExtendsBaseController.Contains(item.ServiceType))
            {
                _serviceClassExtendsBaseController.Add(item.ServiceType);
            }
        }

        var built = services.BuildServiceProvider();
        Services = built;
        RunServicesInitFunc();
    }
    /// <summary>
    /// Used to generate a list of all types that extend <see cref="BaseController"/> in <see cref="Services"/> before it's built.
    /// </summary>
    private static List<Type> _serviceClassExtendsBaseController = new List<Type>();
    /// <summary>
    /// Run the InitializeAsync function on all types in <see cref="Services"/> that extend <see cref="BaseController"/>. Calls <see cref="BaseServiceFunc(Func{BaseController, Task})"/>
    /// </summary>
    private static void RunServicesInitFunc()
    {
        BaseServiceFunc((contr) =>
        {
            contr.InitializeAsync().Wait();
            return Task.CompletedTask;
        });
    }
    /// <summary>
    /// Call the OnReady function on all types in <see cref="Services"/> that extend <see cref="BaseController"/>. Calls <see cref="BaseServiceFunc(Func{BaseController, Task})"/>
    /// </summary>
    private static void RunServicesReadyFunc()
    {
        BaseServiceFunc((contr) =>
        {
            contr.OnReady().Wait();
            return Task.CompletedTask;
        });
    }
    /// <summary>
    /// For every instance of something that extends <see cref="BaseController"/> on <see cref="Services"/>, call <paramref name="func"/> so you can do what you want.
    /// </summary>
    /// <param name="func"></param>
    private static void BaseServiceFunc(Func<BaseController, Task> func)
    {
        var taskList = new List<Task>();
        foreach (var service in _serviceClassExtendsBaseController)
        {
            var svc = Services.GetServices(service);
            foreach (var item in svc)
            {
                if (item != null && item.GetType().IsAssignableTo(typeof(BaseController)))
                {
                    taskList.Add(new Task(delegate
                    {
                        func((BaseController)item).Wait();
                    }));
                }
            }
        }
        foreach (var i in taskList)
            i.Start();
        Task.WaitAll(taskList.ToArray());
    }
    #endregion

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
                options.ClientId = ConfigData.OAuth_ClientId;
                options.ClientSecret = ConfigData.OAuth_ClientSecret;
                
                options.ClaimActions.MapCustomJson("urn:discord:avatar:url", user =>
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "https://cdn.discordapp.com/avatars/{0}/{1}.{2}",
                        user.GetString("id"),
                        user.GetString("avatar"),
                        user.GetString("avatar").StartsWith("a_") ? "gif" : "png"));
            });
        builder.Services.AddServerSideBlazor();
        var app = builder.Build();
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
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