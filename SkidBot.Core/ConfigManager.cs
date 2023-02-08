using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkidBot.Core
{
    public class ConfigManager
    {
        public Config Read()
        {
            var config = new Config();
            if (File.Exists(Location))
            {
                var content = File.ReadAllText(Location);
                config = JsonSerializer.Deserialize<Config>(content, Program.SerializerOptions);
                if (config == null)
                {
                    Log.Error($"Failed to parse config");
                    Log.Error(content);
                    Program.Quit(1);
                }
            }
            else
            {
                Log.Debug("Created config, please populate.");
                Write(config);
            }
            Write(config);
            return config;
        }
        public void Write(Config config)
        {
            var content = JsonSerializer.Serialize(config, Program.SerializerOptions);
            File.WriteAllText(Location, content);
        }
        public string Location => Path.Combine(Directory.GetCurrentDirectory(), "config.json");
        public class Config
        {
            public string DiscordToken = "";
            public bool DeveloperMode = true;
            public ulong DeveloperMode_Server = 0;
            public bool UserWhitelistEnable = false;
            public ulong[] UserWhitelist = Array.Empty<ulong>();
            public string Prefix = "sk.";
            public ulong ErrorChannel = 0;
            public ulong ErrorGuild = 0;
            public string MongoDBServer = "";
        }
    }
}
