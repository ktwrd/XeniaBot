using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using ReactionMessage = Discord.Cacheable<Discord.IUserMessage, ulong>;
using ReactionChannel = Discord.Cacheable<Discord.IMessageChannel, ulong>;

namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
[UsedImplicitly]
public class RoleService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(RoleService));
    private readonly DiscordShardedClient _client;
    private readonly RoleConfigRepository _config;
    private readonly RoleMessageConfigRepository _messageConfig;
    public RoleService(IServiceProvider services)
        : base (services)
    {
        _client = services.GetRequiredService<DiscordShardedClient>();
        _config = services.GetRequiredService<RoleConfigRepository>();
        _messageConfig = services.GetRequiredService<RoleMessageConfigRepository>();
    }

    public override Task OnReady()
    {
        _client.ReactionAdded += _client_ReactionAdded;
        _client.ReactionRemoved += _client_ReactionRemoved;
        return Task.CompletedTask;
    }

    private const string GuildNotFoundForUserTemplate
        = "Guild \"{0}\" ({1}) not found for user \"{2}\" ({3}, {4})";
    private const string MemberNotFoundInGuildTemplate
        = "Member \"{0}\" ({1}, {2}) not found in guild \"{3}\" ({4})";
    private static string GuildNotFoundForUserMessage(IGuildUser user)
        => string.Format(GuildNotFoundForUserTemplate,
                user.Guild.Name, user.Guild.Id,
                user.DisplayName, user.Username, user.Id);
    private static string MemberNotFoundInGuildMessage(
        IGuild guild, IGuildUser user)
        => string.Format(MemberNotFoundInGuildTemplate,
            user.DisplayName, user.Username, user.Id,
            guild.Name, guild.Id);

    public async Task GrantUser(IGuildUser user, RoleConfigModel model)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(user.Guild.Id));
        if (guild == null)
            throw new InvalidOperationException(GuildNotFoundForUserMessage(user));
        var member = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(user.Id));
        if (member == null)
            throw new InvalidOperationException(MemberNotFoundInGuildMessage(guild, user));

        var memberRoleIds = member.Roles.Select(e => e.Id).ToHashSet();

        var targetRole = ExceptionHelper.RetryOnTimedOut(() => guild.GetRole(model.RoleId));

        if (model.BlacklistRoleId != 0)
        {
            var blacklistRole = ExceptionHelper.RetryOnTimedOut(() => guild.GetRole(model.BlacklistRoleId));
            var contains = blacklistRole != null && memberRoleIds.Contains(blacklistRole.Id);
            if (blacklistRole == null)
            {
                _log.Warn($"RoleConfigModel.BlacklistRoleId {model.BlacklistRoleId} not found for Guild \"{guild.Name}\" ({guild.Id}) for User \"{user}\" ({user.Id})");
            }
            if (contains)
            {
                throw new NonfatalException($"You have a blacklisted role (<@&{model.BlacklistRoleId}>)");
            }
        }
        else if (model.RequiredRoleId != 0)
        {
            var whitelistRole = ExceptionHelper.RetryOnTimedOut(() => guild.GetRole(model.RequiredRoleId));
            var contains = whitelistRole != null && memberRoleIds.Contains(whitelistRole.Id);
            if (whitelistRole == null)
            {
                _log.Warn($"RoleConfigModel.RequiredRoleId not found (guild: {model.GuildId}, role: {model.RequiredRoleId})");
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
        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(user.Guild.Id));
        if (guild == null)
            throw new InvalidOperationException($"Guild {user.Guild.Id} not found");
        var member = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(user.Id));
        if (member == null)
            throw new InvalidOperationException($"Member {user.Id} not found in guild {guild.Id}");

        var targetRole = ExceptionHelper.RetryOnTimedOut(() => guild.GetRole(model.RoleId));
        if (targetRole == null) return;

        await ExceptionHelper.RetryOnTimedOut(async () =>
            await member.RemoveRoleAsync(targetRole));
    }

    #region Reaction Handling
    private async Task _client_ReactionRemoved(ReactionMessage message, ReactionChannel channel, SocketReaction reaction)
    {
        var messageConfig = await _messageConfig.Get(reaction.MessageId);

        // Ignore if it doesn't exist
        if (messageConfig == null)
            return;

        // Ignore if the emote isn't a valid reaction role.
        if (!messageConfig.ReactionRoleMap.TryGetValue(reaction.Emote.Name, out var targetRoleConfigId))
            return;
        if (string.IsNullOrWhiteSpace(targetRoleConfigId))
            targetRoleConfigId = string.Empty;

        var roleConfigAll = await _config.GetAll(false, uid: targetRoleConfigId);
        var roleConfig = roleConfigAll?.FirstOrDefault();
        if (roleConfig == null)
            return;
        var validateResult = ValidateReactionObjects(message, reaction, roleConfig, targetRoleConfigId);
        if (!validateResult)
            return;

        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(roleConfig.GuildId));
        var role = ExceptionHelper.RetryOnTimedOut(() => guild.GetRole(roleConfig.RoleId));
        var member = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(reaction.UserId));

        await RevokeUser(member, roleConfig);
    }
    private async Task _client_ReactionAdded(ReactionMessage message, ReactionChannel channel, SocketReaction reaction)
    {
        var messageConfig = await _messageConfig.Get(reaction.MessageId);

        // Ignore if it doesn't exist
        if (messageConfig == null)
            return;

        // Ignore if the emote isn't a valid reaction role.
        if (!messageConfig.ReactionRoleMap.TryGetValue(reaction.Emote.Name, out var targetRoleConfigId))
            return;
        if (string.IsNullOrWhiteSpace(targetRoleConfigId))
            targetRoleConfigId = string.Empty;
        
        var roleConfigAll = await _config.GetAll(false, uid: targetRoleConfigId);
        var roleConfig = roleConfigAll.FirstOrDefault();
        if (roleConfig == null) return;

        var validateResult = ValidateReactionObjects(message, reaction, roleConfig, targetRoleConfigId);
        if (!validateResult)
            return;

        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(roleConfig.GuildId));
        var member = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(reaction.UserId));

        await GrantUser(member, roleConfig);
    }
    private bool ValidateReactionObjects(ReactionMessage message, SocketReaction reaction, RoleConfigModel? model, string targetRoleConfigId)
    {
        if (model == null)
        {
            _log.Error($"Role not found (user: {reaction.UserId}, message: {message.Id}, reaction: {reaction.Emote.Name}, targetConfigRoleId: {targetRoleConfigId})");
            Debugger.Break();
            return false;
        }

        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(model.GuildId));
        if (guild == null)
        {
            _log.Error($"Guild not found {model.GuildId}");
            Debugger.Break();
            return false;
        }
        var role = ExceptionHelper.RetryOnTimedOut(() => guild.GetRole(model.RoleId));
        if (role == null)
        {
            _log.Error($"Target role not found (guild: {model.GuildId}, role: {model.RoleId})");
            Debugger.Break();
            return false;
        }

        var member = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(reaction.UserId));
        if (member == null)
        {
            _log.Error($"Member not found (guild: {model.GuildId}, member: {reaction.UserId})");
            Debugger.Break();
            return false;
        }
        return true;
    }
    #endregion
}
