using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using IdGen;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SkidBot.Core.Controllers;
using SkidBot.Core.Helpers;
using SkidBot.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkidBot.Core
{
    public static class Program
    {
        #region Fields
        public static ConfigManager ConfigManager = null;
        public static ConfigManager.Config Config = null;
        public static HttpClient HttpClient = null;
        public static MongoClient MongoClient = null;
        private static DiscordController _discordController;
        /// <summary>
        /// Created after <see cref="DiscordMain"/> is called in <see cref="MainAsync(string[])"/>
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
        public const string MongoDatabaseName = "shortcake";
        #endregion
        public static IMongoDatabase? GetMongoDatabase()
        {
            return MongoClient.GetDatabase(MongoDatabaseName);
        }

        #region Main
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            MainInit();
            MainInit_ValidateMongo();

            MainAsync(args).Wait();
        }
        private static void MainInit()
        {
            ConfigManager = new ConfigManager();
            Config = ConfigManager.Read();
            IdGenerator = SnowflakeHelper.Create(Config.GeneratorId);
            HttpClient = new HttpClient();
        }
        private static void MainInit_ValidateMongo()
        {
            try
            {
                Log.Debug("Connecting to MongoDB");
                MongoClient = new MongoClient(Config.MongoDBServer);
                MongoClient.StartSession();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to connect to MongoDB Server\n{ex}");
                Quit(1);
            }
        }
        private static async Task MainAsync(string[] args)
        {
            CreateServiceProdiver();
            Log.Debug("Connecting to Discord");
            _discordController = Services.GetRequiredService<DiscordController>();
            _discordController.Ready += (c) =>
            {
                RunServicesReadyFunc();
            };
            await _discordController.Run();

            await Task.Delay(-1);
        }
        #endregion

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var except = (Exception)e.ExceptionObject;
            Console.Error.WriteLine(except);
            if (_discordController.IsReady)
            {
                DiscordHelper.ReportError(except).Wait();
            }
#if DEBUG
            Debugger.Break();
#endif
        }

        public static void Quit(int exitCode = 0)
        {
            BeforeQuit();
            Environment.Exit(exitCode);
        }
        private static void BeforeQuit()
        {
            if (ConfigManager != null && Config != null)
                ConfigManager.Write(Config);
        }

        #region Services
        /// <summary>
        /// Initialize all service-related stuff. <see cref="DiscordController"/> is also created here and added as a singleton to <see cref="Services"/>
        /// </summary>
        private static void CreateServiceProdiver()
        {
            Log.Debug("Initializing Services");
            var dsc = new DiscordSocketClient(DiscordController.GetSocketClientConfig());
            var services = new ServiceCollection();

            services.AddSingleton(IdGenerator)
                .AddSingleton(ConfigManager)
                .AddSingleton(Config)
                .AddSingleton(dsc)
                .AddSingleton<DiscordController>()
                .AddSingleton<CommandService>()
                .AddSingleton<InteractionService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<InteractionHandler>();

            AttributeHelper.InjectControllerAttributes(typeof(Program).Assembly, services);
            ServiceClassExtendsBaseController = new List<Type>();

            foreach (var item in services)
            {
                if (item.ServiceType.IsAssignableTo(typeof(BaseController)) && !ServiceClassExtendsBaseController.Contains(item.ServiceType))
                {
                    ServiceClassExtendsBaseController.Add(item.ServiceType);
                }
            }

            var built = services.BuildServiceProvider();
            Services = built;
            RunServicesInitFunc();
        }
        /// <summary>
        /// Used to generate a list of all types that extend <see cref="BaseController"/> in <see cref="Services"/> before it's built.
        /// </summary>
        private static List<Type> ServiceClassExtendsBaseController = new List<Type>();
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
            foreach (var service in ServiceClassExtendsBaseController)
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
        
        public static IdGenerator IdGenerator;
        public static string GetGuildPrefix(ulong id)
        {
            return Config.Prefix;
        }
    }
}