using Discord;
using Discord.Commands;
using Discord.WebSocket;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Helpers;
using XeniaBot.Data.Models;

namespace XeniaBot.Core.Controllers.BotAdditions
{
    [BotController]
    public class LevelSystemController : BaseController
    {
        private IMongoDatabase _db;
        private DiscordSocketClient _client;
        private Random _random;
        private LevelMemberModelController _memberConfig;
        private LevelSystemGuildConfigController _guildConfig;
        public LevelSystemController(IServiceProvider services)
            : base(services)
        {
            _db = services.GetRequiredService<IMongoDatabase>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _guildConfig = services.GetRequiredService<LevelSystemGuildConfigController>();
            _memberConfig = services.GetRequiredService<LevelMemberModelController>();
            _random = new Random();
            _client.MessageReceived += _client_MessageReceived;
        }

        private async Task _client_MessageReceived(SocketMessage rawMessage)
        {
            // Ignore messages from bots & webhooks
            if (rawMessage.Author.IsBot || rawMessage.Author.IsWebhook)
                return;
            // ensures we don't process system/other bot messages
            if (!(rawMessage is SocketUserMessage message))
            {
                return;
            }
            var context = new SocketCommandContext(_client, message);
            var data = await _memberConfig.Get(message.Author.Id, context.Guild.Id);
            if (data == null)
                data = new LevelMemberModel()
                {
                    UserId = message.Author.Id,
                    GuildId = context.Guild.Id
                };
            await _memberConfig.Set(data);
            var guildConfig = await _guildConfig.Get(context.Guild.Id)
                ?? new LevelSystemGuildConfigModel();
            if (!guildConfig.Enable)
                return;

            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var previousMessageDiff = currentTimestamp - data.LastMessageTimestamp;
            if (previousMessageDiff >= 8000)
            {
                try
                {
                    var result = await GrantXp(data, message);
                    var targetChannel = message.Channel;
                    if (guildConfig != null)
                    {
                        if (guildConfig.LevelUpChannel != null)
                        {
                            var tc = context.Guild.GetTextChannel((ulong)guildConfig.LevelUpChannel);
                            if (tc != null)
                                targetChannel = tc;
                        }

                        if (!guildConfig.ShowLeveUpMessage)
                            targetChannel = null;
                    }
                    if (result.DidLevelUp && targetChannel != null)
                    {
                        await targetChannel.SendMessageAsync(
                            $"<@{message.Author.Id}> You've advanced to *level {result.Metadata.UserLevel}*!");
                    }
                }
                catch (Exception e)
                {
                    await DiscordHelper.ReportError(e, context);
                }
            }
        }

        public class GrantXpResult
        {
            /// <summary>
            /// Did this cause the user to level up
            /// </summary>
            public bool DidLevelUp { get; init; }
            /// <summary>
            /// New XP Metadata
            /// </summary>
            public ExperienceMetadata Metadata { get; init; }
        }
        /// <summary>
        /// Grant user 4 to 16 xp.
        /// </summary>
        /// <param name="model">User XP Data</param>
        /// <param name="message">Message that triggered this event</param>
        /// <returns>Result information. See <see cref="GrantXpResult"/></returns>
        public async Task<GrantXpResult> GrantXp(LevelMemberModel model, SocketUserMessage message)
        {
            var data = await _memberConfig.Get(model.UserId, model.GuildId);
            var amount = (ulong)_random.Next(4, 16);

            // Generate previous and current metadata
            var metadataPrevious = LevelSystemHelper.Generate(data);
            data.Xp += amount;
            var metadata = LevelSystemHelper.Generate(data);

            // Set previous Ids
            data.LastMessageChannelId = message.Channel.Id;
            data.LastMessageId = message.Id;

            bool levelUp = metadataPrevious.UserLevel != metadata.UserLevel;
            if (levelUp)
            {
                OnUserLevelUp(data, metadataPrevious, metadata);
            }

            await _memberConfig.Set(data);
            return new GrantXpResult()
            {
                DidLevelUp = levelUp,
                Metadata = metadata
            };
        }
        protected void OnUserLevelUp(LevelMemberModel model, ExperienceMetadata previous, ExperienceMetadata current)
        {
            if (UserLevelUp != null)
            {
                UserLevelUp?.Invoke(model, previous, current);
            }
        }
        public event ExperienceComparisonDelegate UserLevelUp;
    }
}
