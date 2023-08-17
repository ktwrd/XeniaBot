using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using XeniaBot.Data.Controllers;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;

namespace XeniaBot.Core.Controllers.BotAdditions
{
    [BotController]
    public class ConfessionController : BaseController
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordController _discord;
        private readonly ConfessionConfigController _config;
        public ConfessionController(IServiceProvider services)
            : base(services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _discord = services.GetRequiredService<DiscordController>();
            _config = services.GetRequiredService<ConfessionConfigController>();
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
