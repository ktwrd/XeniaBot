﻿using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ShortcakeBot.Core.Controllers;
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
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ConfigManager = new ConfigManager();
            Config = ConfigManager.Read();
            HttpClient = new HttpClient();
            try
            {
                Log.Debug("Connecting to MongoDB");
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

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var except = (Exception)e.ExceptionObject;
            Console.Error.WriteLine(except);
            if (_discordController.IsReady)
            {
                DiscordHelper.ReportError(except).Wait();
            }
#if DEBUG
            throw except;
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
        private static async Task MainAsync(string[] args)
        {
            CreateServiceProdiver();
            Log.Debug("Connecting to Discord");
            _discordController = Services.GetRequiredService<DiscordController>();
            _discordController.MessageRecieved += _discordController_MessageRecieved;
            await _discordController.Run();

            await Task.Delay(-1);
        }

        private static Task _discordController_MessageRecieved(SocketMessage arg)
        {
            return Task.CompletedTask;
        }

        private static void CreateServiceProdiver()
        {
            Log.Debug("Initializing Services");
            var dsc = new DiscordSocketClient(DiscordController.GetSocketClientConfig());
            var services = new ServiceCollection()
                .AddSingleton(ConfigManager)
                .AddSingleton(Config)
                .AddSingleton(dsc)
                .AddSingleton<DiscordController>()
                .AddSingleton<CommandService>()
                .AddSingleton<InteractionService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<InteractionHandler>()
                .AddSingleton<ConfessionController>();

            var built = services.BuildServiceProvider();
            Services = built;
        }
        public static MessageReference ToMessageReference(ICommandContext context)
        {
            return new MessageReference(context.Message.Id, context.Channel.Id, context.Guild.Id);
        }
        public static MessageReference ToMessageReference(IInteractionContext context)
        {
            return new MessageReference(context.Interaction.Id, context.Channel.Id, context.Guild.Id);
        }
        public static string GetGuildPrefix(ulong id)
        {
            return Config.Prefix;
        }
    }
}