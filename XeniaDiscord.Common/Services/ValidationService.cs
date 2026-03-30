using System.Collections.Frozen;
using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Helpers;

namespace XeniaDiscord.Common.Services;

public class ValidationService
{
    private readonly DiscordSocketClient _discord;
    public ValidationService(IServiceProvider services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
    }

    public async Task<Result<GuildPermissionsResult, FailureData>> Permissions(
        IGuild guild,
        GuildPermission[] required)
    {
        IGuildUser? member = null;
        try
        {
            await ExceptionHelper.RetryOnTimedOut(async() =>
            {
                member = await guild.GetCurrentUserAsync();
            });
            if (member == null)
            {
                return Result.Failure<GuildPermissionsResult, FailureData>(
                    new($"Current user ({_discord.CurrentUser.Id}) is not a member in guild \"{guild.Name}\" ({guild.Id})", null, FailureDataKind.CurrentUserIsNotMember));
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<GuildPermissionsResult, FailureData>(
                new($"Could not find own user ({_discord.CurrentUser.Id}) in guild \"{guild.Name}\" ({guild.Id})", ex, FailureDataKind.GetSelfAsMemberFailure));
        }

        var permissions = member.GuildPermissions.ToList();
        var missing = required.Where(e => !permissions.Contains(e)).ToFrozenSet();
        return new GuildPermissionsResult(guild, missing, required);
    }
    public async Task<Result<ChannelPermissionsResult, FailureData>> Permissions(
        IGuildChannel channel,
        ChannelPermission[] required)
    {
        IGuildUser? member = null;
        try
        {
            await ExceptionHelper.RetryOnTimedOut(async() =>
            {
                member = await channel.Guild.GetCurrentUserAsync();
            });
            if (member == null)
            {
                return Result.Failure<ChannelPermissionsResult, FailureData>(
                    new($"Current user ({_discord.CurrentUser.Id}) is not a member in guild \"{channel.Guild.Name}\" ({channel.Guild.Id})", null, FailureDataKind.CurrentUserIsNotMember));
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<ChannelPermissionsResult, FailureData>(
                new($"Could not find own user ({_discord.CurrentUser.Id}) in guild \"{channel.Guild.Name}\" ({channel.Guild.Id})", ex, FailureDataKind.GetSelfAsMemberFailure));
        }

        var channelPermissions = member.GetPermissions(channel);
        var channelPermissionsList = channelPermissions.ToList();
        var missing = required.Where(e => !channelPermissionsList.Contains(e)).ToFrozenSet();
        return new ChannelPermissionsResult(channel, missing, required);
    }
    public async Task<Result<ChannelPermissionsResult, FailureData>> ChannelPermissions(
        ulong guildId,
        ulong channelId,
        ChannelPermission[] required)
    {
        IGuild? guild = null;
        IGuildChannel? channel = null;
        try
        {
            await ExceptionHelper.RetryOnTimedOut(async() =>
            {
                guild = _discord.GetGuild(guildId);
            });
            if (guild == null)
            {
                return Result.Failure<ChannelPermissionsResult, FailureData>(
                    new($"Could not find guild: {guildId}", null, FailureDataKind.GuildNotFound));
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<ChannelPermissionsResult, FailureData>(
                new($"Failed to get guild: {guildId}", ex, FailureDataKind.GetGuildFailure));
        }
        try
        {
            await ExceptionHelper.RetryOnTimedOut(async() =>
            {
                channel = await guild.GetChannelAsync(channelId);
            });
            if (channel == null)
            {
                return Result.Failure<ChannelPermissionsResult, FailureData>(
                    new($"Could not channel {guildId} in guild \"{guild.Name}\" ({guild.Id})", null, FailureDataKind.ChannelNotFound));
            }

        }
        catch (Exception ex)
        {
            return Result.Failure<ChannelPermissionsResult, FailureData>(
                new($"Failed to get channel {channelId} in guild \"{guild.Name}\" ({guild.Id})", ex, FailureDataKind.GetChannelFailure));
        }
        return await Permissions(channel, required);
    }

    public class GuildPermissionsResult(
        IGuild guild,
        IEnumerable<GuildPermission> missing,
        IEnumerable<GuildPermission> expected)
    {
        public IGuild Guild { get; } = guild;
        public IReadOnlySet<GuildPermission> Missing { get; } = missing.ToFrozenSet();
        public IReadOnlySet<GuildPermission> Expected { get; } = expected.ToFrozenSet();

        public bool AnyMissing => Missing.Count > 0;
    }
    public class ChannelPermissionsResult(
        IGuildChannel channel,
        IEnumerable<ChannelPermission> missing,
        IEnumerable<ChannelPermission> expected)
    {
        public IGuildChannel Channel { get; } = channel;
        public IReadOnlySet<ChannelPermission> Missing { get; } = missing.ToFrozenSet();
        public IReadOnlySet<ChannelPermission> Expected { get; } = expected.ToFrozenSet();

        public bool AnyMissing => Missing.Count > 0;
    }

    public class FailureData(
        string message,
        Exception? exception = null,
        FailureDataKind kind = FailureDataKind.Unknown)
    {
        public string Message { get; } = message;
        public Exception? Exception { get; } = exception;
        public FailureDataKind Kind { get; } = kind;
    }
    public enum FailureDataKind
    {
        Unknown,
        GetGuildFailure,
        GuildNotFound,

        GetChannelFailure,
        ChannelNotFound,

        GetSelfAsMemberFailure,
        CurrentUserIsNotMember,

        Fatal
    }
}