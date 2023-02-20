using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SkidBot.Core.Models;
using SkidBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SkidBot.Core.Controllers
{
    [SkidController]
    public class ConfessionController : BaseController
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordController _discord;
        public ConfessionController(IServiceProvider services)
            : base(services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _discord = services.GetRequiredService<DiscordController>();
        }

        public override Task InitializeAsync()
        {
            _client.ButtonExecuted += _client_ButtonExecuted;
            _client.ModalSubmitted += _client_ModalSubmitted;
            return Task.CompletedTask;
        }

        private async Task _client_ModalSubmitted(SocketModal arg)
        {
            if (arg.Data.CustomId != "confessioncontroller_confess_modal")
            {
                return;
            }
            string content = arg.Data.Components.First(x => x.CustomId == "confession_text").Value;

            var data = await GetGuild(arg.GuildId ?? 0);
            var guild = _client.GetGuild(data.GuildId);
            var channel = guild.GetTextChannel(data.ChannelId);
            await channel.SendMessageAsync(embed: GenerateConfessionEmbed(content).Build());

            await arg.RespondAsync("Done!", ephemeral: true);
        }

        private async Task _client_ButtonExecuted(SocketMessageComponent arg)
        {
            if (!arg.Data.CustomId.StartsWith("confessioncontroller"))
                return;

            switch (arg.Data.CustomId)
            {
                case "confessioncontroller_confess_button":
                    await arg.RespondWithModalAsync(GetModal().Build());
                    break;
            }
        }
        internal async Task InitializeModal(ulong guildId, ulong channelId, ulong modalChannelId)
        {
            var data = await GetGuild(guildId);
            if (data == null)
            {
                data = new ConfessionGuildModel()
                {
                    GuildId = guildId,
                    ChannelId = channelId,
                    ModalChannelId = modalChannelId
                };
                await Set(data);
            }

            var confessionEmbed = new EmbedBuilder()
            {
                Title = "Confessions",
                Description = $"Add anonymous confession to <#{channelId}>"
            };

            var guild = _client.GetGuild(guildId);
            var channel = guild.GetTextChannel(modalChannelId);
            var components = new ComponentBuilder()
                .WithButton("Confess", "confessioncontroller_confess_button", ButtonStyle.Primary);
            
            var message = await channel.SendMessageAsync(embed: confessionEmbed.Build(), components: components.Build());
            data.ModalMessageId = message.Id;
            await Set(data);
        }
        private ModalBuilder GetModal()
        {
            var confessionModal = new ModalBuilder()
                .WithTitle("Confess")
                .WithCustomId("confessioncontroller_confess_modal")
                .AddTextInput("Confession Text", "confession_text", placeholder: "Anonymously Confess", style: TextInputStyle.Paragraph);
            return confessionModal;
        }
        private EmbedBuilder GenerateConfessionEmbed(string content)
        {
            return new EmbedBuilder()
            {
                Title = "Confession",
                Description = content,
                Timestamp = DateTimeOffset.UtcNow,
                Color = new Color(255, 255, 255)
            };
        }
        #region MongoDB Boilerplate
        public const string MongoCollectionName = "confesionGuildModel";
        protected static IMongoCollection<T>? GetCollection<T>()
        {
            return Program.GetMongoDatabase()?.GetCollection<T>(MongoCollectionName);
        }
        protected static IMongoCollection<ConfessionGuildModel>? GetCollection()
            => GetCollection<ConfessionGuildModel>();
        internal async Task<ConfessionGuildModel?> GetGuild(ulong guildId)
        {
            var collection = GetCollection();
            var filter = Builders<ConfessionGuildModel>
                .Filter
                .Eq("GuildId", guildId);

            var results = await collection.FindAsync(filter);

            return results.FirstOrDefault();
        }
        internal async Task Set(ConfessionGuildModel model)
        {
            var collection = GetCollection();
            var filter = Builders<ConfessionGuildModel>
                .Filter
                .Eq("GuildId", model.GuildId);

            var existingItems = await collection.FindAsync(filter);
            if (existingItems != null && existingItems.Any())
                await collection.FindOneAndReplaceAsync(filter, model);
            else
                await collection.InsertOneAsync(model);
        }
        internal async Task Delete(ConfessionGuildModel model)
        {
            var guild = _client.GetGuild(model.GuildId);
            if (guild == null)
                throw new Exception($"Guild {model.GuildId} not found");
            var channel = guild.GetTextChannel(model.ModalChannelId);
            if (channel == null)
                throw new Exception($"Channel {model.ModalChannelId} not found");
            await channel.DeleteMessageAsync(model.ModalMessageId);

            var collection = GetCollection();
            var filter = Builders<ConfessionGuildModel>
                .Filter
                .Eq("GuildId", model.GuildId);

            await collection?.DeleteManyAsync(filter);
        }
        #endregion
    }
}
