using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Models;
using SkidBot.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using ReactionMessage = Discord.Cacheable<Discord.IUserMessage, ulong>;
using ReactionChannel = Discord.Cacheable<Discord.IMessageChannel, ulong>;


namespace SkidBot.Core.Controllers.BotAdditions
{
    [SkidController]
    public class RoleController : BaseController
    {
        private DiscordSocketClient _client;
        private RoleConfigController _config;
        private RoleMessageConfigController _messageConfig;
        public RoleController(IServiceProvider services)
            : base (services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _config = services.GetRequiredService<RoleConfigController>();
            _messageConfig = services.GetRequiredService<RoleMessageConfigController>();
        }

        public override Task OnReady()
        {
            _client.ReactionAdded += _client_ReactionAdded;
            _client.ReactionRemoved += _client_ReactionRemoved;
            return Task.CompletedTask;
        }

        public async Task GrantUser(IGuildUser user, RoleConfigModel model)
        {
            var guild = _client.GetGuild(user.Guild.Id);
            if (guild == null)
                throw new Exception($"Guild {user.Guild.Id} not found");
            var member = guild.GetUser(user.Id);
            if (member == null)
                throw new Exception($"Member {user.Id} not found in guild {guild.Id}");

            var targetRole = guild.GetRole(model.RoleId);

            if (model.BlacklistRoleId != 0)
            {
                var blacklistRoleId = guild.GetRole(model.BlacklistRoleId);
                var contains = blacklistRoleId == null ? false : member.Roles.Contains(blacklistRoleId);
                if (blacklistRoleId == null)
                {
                    Log.Warn($"RoleConfigModel.BlacklistRoleId not found (guild: {model.GuildId}, role: {model.BlacklistRoleId})");
                }
                if (contains)
                {
                    throw new NonfatalException($"You have a blacklisted role (<@&{model.BlacklistRoleId}>)");
                }
            }
            else if (model.RequiredRoleId != 0)
            {
                var whitelistRoleId = guild.GetRole(model.RequiredRoleId);
                var contains = whitelistRoleId == null ? false : member.Roles.Contains(whitelistRoleId);
                if (whitelistRoleId == null)
                {
                    Log.Warn($"RoleConfigModel.RequiredRoleId not found (guild: {model.GuildId}, role: {model.RequiredRoleId})");
                }
                if (!contains)
                {
                    throw new NonfatalException($"You must have <@&{model.RequiredRoleId}> to continue");
                }
            }

            await member.AddRoleAsync(targetRole);
        }
        public async Task RevokeUser(IGuildUser user, RoleConfigModel model)
        {
            var guild = _client.GetGuild(user.Guild.Id);
            if (guild == null)
                throw new Exception($"Guild {user.Guild.Id} not found");
            var member = guild.GetUser(user.Id);
            if (member == null)
                throw new Exception($"Member {user.Id} not found in guild {guild.Id}");

            var targetRole = guild.GetRole(model.RoleId);

            await member.RemoveRoleAsync(targetRole);
        }

        #region Reaction Handling
        private async Task _client_ReactionRemoved(ReactionMessage message, ReactionChannel channel, SocketReaction reaction)
        {
            var messageConfig = await _messageConfig.Get(reaction.MessageId);

            // Ignore if doesn't exist
            if (messageConfig == null)
                return;

            // Ignore if the emote isn't a valid reaction role.
            if (!messageConfig.ReactionRoleMap.ContainsKey(reaction.Emote.Name))
                return;

            var targetRoleConfigId = messageConfig.ReactionRoleMap[reaction.Emote.Name] ?? "";
            var roleConfigAll = await _config.GetAll(false, uid: targetRoleConfigId);
            var roleConfig = roleConfigAll?.FirstOrDefault();

            var validateResult = ValidateReactionObjects(message, reaction, roleConfig, targetRoleConfigId);
            if (!validateResult)
                return;

            var guild = _client.GetGuild(roleConfig.GuildId);
            var role = guild.GetRole(roleConfig.RoleId);
            var member = guild.GetUser(reaction.UserId);

            await RevokeUser(member, roleConfig);
        }
        private async Task _client_ReactionAdded(ReactionMessage message, ReactionChannel channel, SocketReaction reaction)
        {
            var messageConfig = await _messageConfig.Get(reaction.MessageId);

            // Ignore if doesn't exist
            if (messageConfig == null)
                return;

            // Ignore if the emote isn't a valid reaction role.
            if (!messageConfig.ReactionRoleMap.ContainsKey(reaction.Emote.Name))
                return;

            var targetRoleConfigId = messageConfig.ReactionRoleMap[reaction.Emote.Name] ?? "";
            var roleConfigAll = await _config.GetAll(false, uid: targetRoleConfigId);
            var roleConfig = roleConfigAll?.FirstOrDefault();

            var validateResult = ValidateReactionObjects(message, reaction, roleConfig, targetRoleConfigId);
            if (!validateResult)
                return;

            var guild = _client.GetGuild(roleConfig.GuildId);
            var member = guild.GetUser(reaction.UserId);

            await GrantUser(member, roleConfig);
        }
        private bool ValidateReactionObjects(ReactionMessage message, SocketReaction reaction, RoleConfigModel model, string targetRoleConfigId)
        {
            if (model == null)
            {
                Log.Error($"Role not found (user: {reaction.UserId}, message: {message.Id}, reaction: {reaction.Emote.Name}, targetConfigRoleId: {targetRoleConfigId})");
                Debugger.Break();
                return false;
            }

            var guild = _client.GetGuild(model.GuildId);
            if (guild == null)
            {
                Log.Error($"Guild not found {model.GuildId}");
                Debugger.Break();
                return false;
            }
            var role = guild.GetRole(model.RoleId);
            if (role == null)
            {
                Log.Error($"Target role not found (guild: {model.GuildId}, role: {model.RoleId})");
                Debugger.Break();
                return false;
            }

            var member = guild.GetUser(reaction.UserId);
            if (member == null)
            {
                Log.Error($"Member not found (guild: {model.GuildId}, member: {reaction.UserId})");
                Debugger.Break();
                return false;
            }
            return true;
        }
        #endregion
    }
}
