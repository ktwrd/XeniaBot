using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using ReactionMessage = Discord.Cacheable<Discord.IUserMessage, ulong>;
using ReactionChannel = Discord.Cacheable<Discord.IMessageChannel, ulong>;
using NLog;


namespace XeniaBot.Core.Services.BotAdditions;

[XeniaController]
public class RoleService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(RoleService));
    private DiscordSocketClient _client;
    private RoleConfigRepository _config;
    private RoleMessageConfigRepository _messageConfig;
    public RoleService(IServiceProvider services)
        : base (services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<RoleConfigRepository>();
        _messageConfig = services.GetRequiredService<RoleMessageConfigRepository>();
    }

    public override Task OnReady()
    {
        _client.ReactionAdded += _client_ReactionAdded;
        _client.ReactionRemoved += _client_ReactionRemoved;
        return Task.CompletedTask;
    }

    private const string GuildNotFoundForUserTempl
        = "Guild \"{0}\" ({1}) not found for user \"{2}\" ({3}, {4})";
    private const string MemberNotFoundInGuildTempl
        = "Member \"{0}\" ({1}, {2}) not found in guild \"{3}\" ({4})";
    private static string GuildNotFoundForUserMessage(IGuildUser user)
        => string.Format(GuildNotFoundForUserTempl,
                user.Guild.Name, user.Guild.Id,
                user.DisplayName, user.Username, user.Id);
    private static string MemberNotFoundInGuildMessage(
        IGuild guild, IGuildUser user)
        => string.Format(MemberNotFoundInGuildTempl,
            user.DisplayName, user.Username, user.Id,
            guild.Name, guild.Id);

    public async Task GrantUser(IGuildUser user, RoleConfigModel model)
    {
        var guild = _client.GetGuild(user.Guild.Id);
        if (guild == null)
            throw new InvalidOperationException(GuildNotFoundForUserMessage(user));
        var member = guild.GetUser(user.Id);
        if (member == null)
            throw new InvalidOperationException(MemberNotFoundInGuildMessage(guild, user));

        var memberRoleIds = member.Roles.Select(e => e.Id).ToHashSet();

        var targetRole = await guild.GetRoleAsync(model.RoleId);

        if (model.BlacklistRoleId != 0)
        {
            var blacklistRole = await guild.GetRoleAsync(model.BlacklistRoleId);
            var contains = blacklistRole == null ? false : memberRoleIds.Contains(blacklistRole.Id);
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
            var whitelistRole = await guild.GetRoleAsync(model.RequiredRoleId);
            var contains = whitelistRole == null ? false : memberRoleIds.Contains(whitelistRole.Id);
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
        var guild = _client.GetGuild(user.Guild.Id);
        if (guild == null)
            throw new Exception($"Guild {user.Guild.Id} not found");
        var member = guild.GetUser(user.Id);
        if (member == null)
            throw new Exception($"Member {user.Id} not found in guild {guild.Id}");

        var targetRole = await guild.GetRoleAsync(model.RoleId);

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
        if (roleConfig == null)
            return;
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
            _log.Error($"Role not found (user: {reaction.UserId}, message: {message.Id}, reaction: {reaction.Emote.Name}, targetConfigRoleId: {targetRoleConfigId})");
            Debugger.Break();
            return false;
        }

        var guild = _client.GetGuild(model.GuildId);
        if (guild == null)
        {
            _log.Error($"Guild not found {model.GuildId}");
            Debugger.Break();
            return false;
        }
        var role = guild.GetRole(model.RoleId);
        if (role == null)
        {
            _log.Error($"Target role not found (guild: {model.GuildId}, role: {model.RoleId})");
            Debugger.Break();
            return false;
        }

        var member = guild.GetUser(reaction.UserId);
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
