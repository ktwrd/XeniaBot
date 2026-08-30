using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using System.Collections.Frozen;
using XeniaBot.Shared.Helpers;

namespace XeniaDiscord.Interactions.Modules;

partial class DeveloperModule
{
    private async Task<Result<ValidateChannelPermissionsResult, ValidatePermissionsError>> ValidatePermissions(
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
            guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(guildId));
            if (guild == null)
            {
                return new ValidatePermissionsError($"Guild not found: `{guildId}`", null);
            }
        }
        catch (Exception ex)
        {
            return new ValidatePermissionsError($"Failed to get Guild: `{guildId}`", ex);
        }
        try
        {
            channel = ExceptionHelper.RetryOnTimedOut(() => guild.GetChannel(channelId));
            if (channel == null)
            {
                return new ValidatePermissionsError($"Channel `{channelId}` not found in Guild `{guildId}`", null);
            }
        }
        catch (Exception ex)
        {
            return new ValidatePermissionsError($"Failed to get Channel `{channelId}` Guild `{guildId}`", ex);
        }
        try
        {
            member = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(_client.CurrentUser.Id));
            if (member == null)
            {
                return new ValidatePermissionsError($"Member `{_client.CurrentUser.Id}` (me) not found in Guild `{guildId}`", null);
            }
        }
        catch (Exception ex)
        {
            return new ValidatePermissionsError($"Failed to get own user in Guild `{guildId}`", ex);
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

    internal sealed record ValidatePermissionsError(string Message, Exception? Exception);
    
    internal class ValidateChannelPermissionsResult(
        IEnumerable<ChannelPermission> permissions)
    {
        public IReadOnlySet<ChannelPermission> Missing { get; } = permissions.ToFrozenSet();
        public IReadOnlySet<GuildPermission> GuildMissing { get; init; } = Array.Empty<GuildPermission>().ToFrozenSet();

        private const string TextNothing = "No issues found.";
        private const string EmoteAlert = "❗";
        private const string EmoteOk = "✔️";
        public string GetMissingText()
        {
            if (Missing.Count == 0) return TextNothing;
            return string.Join("\n",
                $"Missing {Missing.Count} permission(s)",
                "```",
                string.Join("\n", Missing.Select(e => e.ToString())),
                "```");
        }

        public string GetGuildMissingText()
        {
            if (GuildMissing.Count == 0) return TextNothing;
            return string.Join("\n",
                $"Missing {GuildMissing.Count} permission(s)",
                "```",
                string.Join("\n", GuildMissing.Select(e => e.ToString())),
                "```");
        }
        
        public void AddEmbedFields(EmbedBuilder embed)
        {
            var channelText = GetMissingText();
            var channelTitle = Missing.Count == 0
                ? $"{EmoteOk} Channel"
                : $"{EmoteAlert} Channel";
            embed.AddField(channelTitle, channelText);

            var guildText = GetGuildMissingText();
            var guildTitle = GuildMissing.Count == 0
                ? $"{EmoteOk} Guild"
                : $"{EmoteAlert} Guild";
            embed.AddField(guildTitle, guildText);

            var color = GuildMissing.Count == 0 && Missing.Count == 0
                ? Color.Green
                : Color.Orange;
            embed.WithColor(color);
        }
    }
}
