using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using System.Collections.Frozen;

namespace XeniaDiscord.Interactions.Modules;

partial class DeveloperModule
{
    private async Task<Result<ValidateChannelPermissionsResult, (string, Exception?)>> ValidatePermissions(
        ulong guildId,
        ulong channelId,
        ChannelPermission[] expected,
        GuildPermission[]? expectedGuild = null)
    {
        SocketGuild? guild = null;
        SocketGuildChannel? channel = null;
        SocketGuildUser? member = null;
        try
        {
            guild = _client.GetGuild(guildId);
            if (guild == null)
            {
                return Result.Failure<ValidateChannelPermissionsResult, (string, Exception?)>(($"Guild not found: `{guildId}`", null));
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<ValidateChannelPermissionsResult, (string, Exception?)>(($"Failed to get Guild: `{guildId}`", ex));
        }
        try
        {
            channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                return Result.Failure<ValidateChannelPermissionsResult, (string, Exception?)>(($"Channel `{channelId}` not found in Guild `{guildId}`", null));
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<ValidateChannelPermissionsResult, (string, Exception?)>(($"Failed to get Channel `{channelId}` Guild `{guildId}`", ex));
        }
        try
        {
            member = guild.GetUser(_client.CurrentUser.Id);
            if (member == null)
            {
                return Result.Failure<ValidateChannelPermissionsResult, (string, Exception?)>(($"Member `{_client.CurrentUser.Id}` (me) not found in Guild `{guildId}`", null));
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<ValidateChannelPermissionsResult, (string, Exception?)>(($"Failed to get own user in Guild `{guildId}`", ex));
        }

        var channelPermissions = member.GetPermissions(channel);
        var channelPermissionsList = channelPermissions.ToList();

        var missingGuildPermissions = Array.Empty<GuildPermission>();
        if (expectedGuild != null)
        {
            var guildPermissionsList = member.GuildPermissions.ToList();
            missingGuildPermissions = expectedGuild.Where(e => !guildPermissionsList.Contains(e)).ToArray();
        }

        return new ValidateChannelPermissionsResult(expected.Where(e => !channelPermissionsList.Contains(e)))
        {
            GuildMissing = missingGuildPermissions.ToFrozenSet()
        };
    }

    internal class ValidateChannelPermissionsResult(
        IEnumerable<ChannelPermission> permissions)
    {
        public IReadOnlySet<ChannelPermission> Missing { get; } = permissions.ToFrozenSet();
        public IReadOnlySet<GuildPermission> GuildMissing { get; init; } = Array.Empty<GuildPermission>().ToFrozenSet();

        public void AddEmbedFields(EmbedBuilder embed)
        {
            const string nothing = "No issues found.";
            const string alert = "❗";
            const string ok = "✔️";
            if (Missing.Count < 1)
            {
                embed.AddField($"{ok} Channel", nothing);
            }
            else
            {
                embed.AddField($"{alert} Channel",
                    string.Join("\n",
                    $"Missing {Missing.Count} permission(s)",
                    "```",
                    string.Join("\n", Missing.Select(e => e.ToString())),
                    "```"));
            }
            if (GuildMissing.Count < 1)
            {
                embed.AddField($"{ok} Guild", nothing);
            }
            else
            {
                embed.AddField($"{alert} Guild",
                    string.Join("\n",
                    $"Missing {GuildMissing.Count} permission(s)",
                    "```",
                    string.Join("\n", GuildMissing.Select(e => e.ToString())),
                    "```"));
            }

            var color = GuildMissing.Count == 0 && Missing.Count == 0
                ? Color.Green
                : Color.Orange;
            embed.WithColor(color);
        }
    }
}
