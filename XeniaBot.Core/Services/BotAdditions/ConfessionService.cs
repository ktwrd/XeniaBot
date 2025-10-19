using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Data.Repositories;

namespace XeniaBot.Core.Services.BotAdditions
{
    [XeniaController]
    public class ConfessionService : BaseService
    {
        private readonly DiscordSocketClient _client;
        private readonly ConfessionConfigRepository _config;
        public ConfessionService(IServiceProvider services)
            : base(services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _config = services.GetRequiredService<ConfessionConfigRepository>();
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

            var data = await _config.GetGuild(arg.GuildId ?? 0);
            if (data == null)
            {
                await arg.RespondAsync("Couldn't find configuration for guild " + arg.GuildId.ToString());
                return;
            }
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
            await _config.InitializeModal(guildId, channelId, modalChannelId);
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
    }
}
