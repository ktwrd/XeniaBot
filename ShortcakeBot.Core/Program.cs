using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ShortcakeBot.Core.Helpers;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShortcakeBot.Core
{
    public static class Program
    {
        #region Fields
        public static ConfigManager ConfigManager = null;
        public static ConfigManager.Config Config = null;
        public static HttpClient HttpClient = null;
        public static MongoClient MongoClient = null;
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
        #endregion
        public const string MongoDatabaseName = "shortcake";
        public static IMongoDatabase? GetMongoDatabase()
        {
            return MongoClient.GetDatabase(MongoDatabaseName);
        }
        /// <summary>
        /// UTC of <see cref="DateTimeOffset.ToUnixTimeSeconds()"/>
        /// </summary>
        public static long StartTimestamp { get; private set; }
        public static void Main(string[] args)
        {
            StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ConfigManager = new ConfigManager();
            Config = ConfigManager.Read();
            HttpClient = new HttpClient();
            try
            {
                MongoClient = new MongoClient(Config.MongoDBServer);
                MongoClient.StartSession();
            }
            catch(Exception ex)
            {
                Log.Error($"Failed to connect to MongoDB Server\n{ex}");
                Quit(1);
            }

            MainAsync(args).Wait();
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
        private static async Task MainAsync(string[] args)
        {
            await DiscordMain();
            CreateServiceProdiver();

            await Task.Delay(-1);
        }
        private static void CreateServiceProdiver()
        {
            var services = new ServiceCollection()
                .AddSingleton(DiscordSocketClient)
                .AddSingleton<CommandService>()
                .AddSingleton<InteractionService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<InteractionHandler>();

            var built = services.BuildServiceProvider();
            Services = built;
        }
        public static async Task DiscordReady()
        {
            await Services.GetRequiredService<CommandHandler>()?.InitializeAsync();
            Services.GetRequiredService<InteractionHandler>();
        }
        public static MessageReference ToMessageReference(ICommandContext context)
        {
            return new MessageReference(context.Message.Id, context.Channel.Id, context.Guild.Id);
        }
        public static MessageReference ToMessageReference(IInteractionContext context)
        {
            return new MessageReference(context.Interaction.Id, context.Channel.Id, context.Guild.Id);
        }
        

        #region Discord Boilerplate
        public static DiscordSocketClient DiscordSocketClient = null;
        private static async Task DiscordMain()
        {
            DiscordSocketClient = new DiscordSocketClient(new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers,
                UseInteractionSnowflakeDate = false,
                HandlerTimeout = 6000
            });
            DiscordSocketClient.Log += DiscordSocketClient_Log;
            DiscordSocketClient.Ready += DiscordSocketClient_Ready;
            await DiscordSocketClient.LoginAsync(TokenType.Bot, Config.DiscordToken);
            await DiscordSocketClient.StartAsync();

            DiscordSocketClient.MessageReceived += DiscordSocketClient_MessageReceived;
        }

        private static bool Ready = false;
        private static async Task DiscordSocketClient_Ready()
        {
            Ready = true;
            await DiscordReady();
            CounterHelper.Ready();
        }

        private static Task DiscordSocketClient_MessageReceived(SocketMessage arg)
        {
            CounterHelper.MessageRecieved(arg);
            return Task.CompletedTask;
        }

        public static string GetGuildPrefix(ulong id)
        {
            return Config.Prefix;
        }

        private static Task DiscordSocketClient_Log(Discord.LogMessage arg)
        {
            switch (arg.Severity)
            {
                case Discord.LogSeverity.Debug:
                    Log.Debug(arg.ToString());
                    break;
                case Discord.LogSeverity.Verbose:
                    Log.Debug(arg.ToString());
                    break;
                case Discord.LogSeverity.Info:
                    Log.WriteLine(arg.ToString());
                    break;
                case Discord.LogSeverity.Warning:
                    Log.Warn(arg.ToString());
                    break;
                case Discord.LogSeverity.Error:
                    Log.Error(arg.ToString());
                    break;
                case Discord.LogSeverity.Critical:
                    Log.Error(arg.ToString());
                    break;
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}