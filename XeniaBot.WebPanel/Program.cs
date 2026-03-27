using CronNET;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using MongoDB.Driver;
using NLog;
using NLog.Web;
using Sentry;
using System;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord;
using XeniaDiscord.Common;

namespace XeniaBot.WebPanel;

public static class Program
{
    #region Properties
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        IncludeFields = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve
    };
    /// <summary>
    /// UTC of <see cref="DateTimeOffset.ToUnixTimeSeconds()"/>
    /// </summary>
    public static long StartTimestamp { get; private set; }
    public static string VersionFull => $"v{Version?.Major}.{Version?.Minor} ({VersionDate})";
    public static DateTime VersionDate
    {
        get
        {
            var v = Assembly.GetAssembly(typeof(Program))?.GetName().Version;
            DateTime buildDate = ProgramDetails.VersionDateEpoch
                .AddDays(v?.Build ?? 0)
                .AddSeconds((v?.Revision ?? 0) * 2);
            return buildDate;
        }
    }
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
    public static ProgramDetails Details => new ProgramDetails()
    {
        VersionRaw = Version,
        StartTimestamp = StartTimestamp,
        Platform = XeniaPlatform.WebPanel,
        Debug = Debug
    };
#if DEBUG
    private const bool Debug = true;
#else
    private const bool Debug = false;
#endif
    public static CoreContext Core { get; private set; }
    #endregion
    public static void Main(string[] args)
    {
        StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        LogManager.Setup().LoadConfigurationFromFile(FeatureFlags.NLogFileLocation);
        if (!string.IsNullOrEmpty(FeatureFlags.SentryDSN))
        {
            SentrySdk.Init(static options =>
            {
                Update(options);
            });
            LogManager.Configuration?.AddSentry(options =>
            {
                Update(options);
            });
        }

        LogManager.GetLogger("Main").Info($"Running version {Details.VersionRaw}");
        Core = new CoreContext(Details)
        {
            StartTimestamp = StartTimestamp,
            AlternativeMain = CoreContextAlternativeMain
        };
        try
        {
            Core.MainAsync(args, CoreContextBeforeServiceBuild).GetAwaiter().GetResult();
        }
        finally
        {
            LogManager.Shutdown();
            SentrySdk.Flush();
        }
    }
    private static void Update(SentryOptions options)
    {
        options.Dsn = FeatureFlags.SentryDSN;
        options.Release = Version?.ToString();
        options.SendDefaultPii = true;
        options.AttachStacktrace = true;
        options.Environment = Details.Debug ? "production" : "debug";
        options.TracesSampleRate = 1.0;
        options.IsGlobalModeEnabled = false;
        options.Debug = Details.Debug;
    }
    private static async Task CoreContextAlternativeMain(string[] args)
    {
        await Main_AspNet(args);
        await Task.Delay(-1);
    }
    private static Task CoreContextBeforeServiceBuild(IServiceCollection services)
    {
        services.WithDatabaseServices();
        services.AddSingleton(Core);
        services.AddSingleton(Details);
        XeniaDiscordData.RegisterServices(services, false); // only allow scoped db stuff for web app
        XeniaDiscordCommon.RegisterServices(services, false);
        XeniaDiscordInteractionsDataMigration.RegisterServices(services);
        AttributeHelper.InjectControllerAttributes(typeof(XeniaHelper).Assembly, services); // XeniaBot.Shared
        AttributeHelper.InjectControllerAttributes(typeof(XeniaVersionRepository).Assembly, services); // XeniaBot.Data
        AttributeHelper.InjectControllerAttributes("XeniaBot.WebPanel", services);
        return Task.CompletedTask;
    }

    public static async Task Main_AspNet(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // NLog: Setup NLog for Dependency injection
        builder.Logging.ClearProviders();
        builder.Host.UseNLog();

        // Add services to the container.
        builder.Services.AddMvc();
        builder.Services.AddControllersWithViews().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.WriteIndented = true;
        });
        builder.Services
            .AddAuthentication(options =>
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
                options.ClientId = Core.Config.Data.OAuthId;
                options.ClientSecret = Core.Config.Data.OAuthSecret;

                options.ClaimActions.MapCustomJson("urn:discord:avatar:url", user =>
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "https://cdn.discordapp.com/avatars/{0}/{1}.{2}",
                        user.GetString("id"),
                        user.GetString("avatar"),
                        (user.GetString("avatar")?.StartsWith("a_") ?? false) ? "gif" : "png"));
            });
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddResponseCompression(options =>
        {
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
                "application/json", "application/geo+json",
                "application/json", "application/pdf",
                "application/sql", "application/toml",
                "application/octet-stream", "application/xml",
                "application/wasm",
                "application/font-woff2",

                "audio/aac", "audio/flac",
                "audio/mp4", "audio/mpeg",
                "audio/ogg", "audio/opus",
                "audio/vorbis",
                "audio/midi", "audio/x-midi",

                "image/apng", "image/avif", "image/bmp",
                "image/gif", "image/heic", "image/heic-sequence",
                "image/heif", "image/heif-sequence",
                "image/jpeg", "image/jxl", "image/png",
                "image/svg+xml", "image/tiff", "image/tiff-fx",
                "image/webp", "image/vnd.microsoft.icon",

                "model/mtl", "model/mesh", "model/obj",

                "application/font-woff2",
                "font/collection", "font/otf",
                "font/sfnt", "font/ttf",
                "font/woff", "font/woff2",

                "text/javascript", "text/css",
                "text/css", "text/rtf",
                "text/plain", "text/xml",
                "text/markdown",


                "video/mp4", "video/av1",
                "video/mpeg", "video/mpeg",
                "video/ogg", "video/quicktime",
                "video/webm"
            ]);
            options.EnableForHttps = true;
        });
        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.SmallestSize;
        });
        builder.WebHost.UseSentry(FeatureFlags.SentryDSN);

        builder.Services.AddSingleton(Core.Services.GetRequiredService<CoreContext>());
        builder.Services.AddSingleton(Core.Services.GetRequiredService<ProgramDetails>());
        builder.Services.AddSingleton(Core.Services.GetRequiredService<CronDaemon>());
        builder.Services.AddSingleton(Core.Services.GetRequiredService<ConfigService>());
        builder.Services.AddSingleton(Core.Services.GetRequiredService<ConfigData>());
        builder.Services.AddSingleton(Core.Services.GetRequiredService<DiscordSocketClient>());
        builder.Services.AddSingleton<IDiscordClient>(Core.Services.GetRequiredService<DiscordSocketClient>());
        builder.Services.AddSingleton(Core.Services.GetRequiredService<IMongoDatabase>());
        builder.Services.AddSingleton(Core.Services.GetRequiredService<DiscordService>());
        await CoreContextBeforeServiceBuild(builder.Services);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        }

        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
            IdentityModelEventSource.ShowPII = true;
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
            app.UseSentryTracing();
        }

        // Required to serve files with no extension in the .well-known folder
        app.UseStaticFiles(new StaticFileOptions()
        {
            ServeUnknownFileTypes = true,
        });

        app.UseResponseCompression();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.UseEndpoints(endpointBuilder =>
        {
            endpointBuilder.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            endpointBuilder.MapRazorPages();
        });

        await app.RunAsync();
    }
}