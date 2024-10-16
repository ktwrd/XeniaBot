using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using XeniaBot.Data;
using XeniaBot.Data.Services;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.Shared.Helpers;
using Microsoft.AspNetCore.Hosting;

public static class Program
{

    #region Fields
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
    #endregion
    public static ProgramDetails Details => new ProgramDetails()
    {
        VersionRaw = Version,
        StartTimestamp = StartTimestamp,
        Platform = XeniaPlatform.WebPanel,
        Debug =
#if DEBUG
            true
#else
            false
#endif
    };
    public static CoreContext Core { get; private set; }
    public static void Main(string[] args)
    {
        StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Log.WriteLine($"Running version {Details.VersionRaw}");
        Core = new CoreContext(Details);
        Core.StartTimestamp = StartTimestamp;
        Core.AlternativeMain = async (a) =>
        {
            await Main_AspNet(a);
            await Task.Delay(-1);
        };
        Core.MainAsync(args, (s) =>
        {
            AttributeHelper.InjectControllerAttributes(typeof(XeniaHelper).Assembly, s); // XeniaBot.Shared
            AttributeHelper.InjectControllerAttributes(typeof(BanSyncService).Assembly, s); // XeniaBot.Data
            AttributeHelper.InjectControllerAttributes("XeniaBot.WebPanel", s);
            return Task.CompletedTask;
        }).Wait();
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
    public static string VersionFull => $"v{Version?.Major}.{Version?.Minor} ({VersionDate})";
    public static DateTime VersionDate
    {
        get
        {
            var v = Assembly.GetAssembly(typeof(Program))?.GetName().Version;
            DateTime buildDate = new DateTime(2000, 1, 1)
                .AddDays(v?.Build ?? 0)
                .AddSeconds((v?.Revision ?? 0) * 2);
            return buildDate;
        }
    }

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
        builder.Services.AddServerSideBlazor();
        builder.WebHost.UseSentry(FeatureFlags.SentryDSN);
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