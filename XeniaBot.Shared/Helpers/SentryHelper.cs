using Discord;
using Discord.WebSocket;
using NLog;
using Sentry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace XeniaBot.Shared.Helpers;

public static class SentryHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static ISpan CreateTransaction(
        ISpan parentTransaction,
        [CallerMemberName] string? methodName = null,
        [CallerFilePath] string? methodFile = null,
        [CallerLineNumber] int? methodLineNumber = null)
    {
        var result = parentTransaction.StartChild(methodName ?? "<unknown_method_name>");
        result.SetTag("method.name", methodName ?? "<unknown_method_name>");
        result.SetTag("method.file", methodFile ?? "<unknown_filename>");
        result.SetTag("method.lineNumber", methodLineNumber?.ToString() ?? "<unknown_linenumber>");
        return result;
    }
    public static ITransactionTracer CreateTransaction(
        [CallerMemberName] string? methodName = null,
        [CallerFilePath] string? methodFile = null,
        [CallerLineNumber] int? methodLineNumber = null)
    {
        var ctx = new TransactionContext(
            Path.GetFileNameWithoutExtension(methodFile) ?? "<unknown_filename>",
            methodName ?? "<unknown_method_name>");
        var result = SentrySdk.StartTransaction(ctx, new Dictionary<string, object?>().AsReadOnly());
        result.SetTag("method.name", methodName ?? "<unknown_method_name>");
        result.SetTag("method.file", methodFile ?? "<unknown_filename>");
        result.SetTag("method.lineNumber", methodLineNumber?.ToString() ?? "<unknown_linenumber>");
        return result;
    }

    public static void SetInteractionInfo(this Scope scope, IDiscordInteraction? interaction)
    {
        if (interaction == null) return;

        var tags = new Dictionary<string, string>();
        var extra = new Dictionary<string, object?>();
        if (interaction.Data is IApplicationCommandInteractionData data)
        {
            tags["interaction.name"] = data.Name;
            tags["interaction.id"] = data.Id.ToString();

            var optionIndex = 0;
            foreach (var option in data.Options)
            {
                if (optionIndex == 0 && option.Type == ApplicationCommandOptionType.SubCommand)
                {
                    tags["interaction.group"] = data.Name;
                    tags["interaction.name"] = option.Name;
                    tags["command.name"] = option.Name;
                    tags["command.group"] = data.Name;
                }

                var p = $"interaction.data.options[{optionIndex}]";
                extra[p + ".name"] = option.Name;
                extra[p + ".options.count"] = option.Options.Count;
                extra[p + ".type"] = option.Type.ToString();
                try
                {
                    extra[p + ".value"] = option.Value;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to set value for interaction {data.Id} (value type: {option.Value?.GetType()})" + string.Join("\n", tags.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                }

                optionIndex++;
            }

            if (optionIndex == 0)
            {
                tags["command.name"] = data.Name;
            }
        }


        tags["channel.id"] = interaction.ChannelId.ToString() ?? "";
        tags["guild.id"] = interaction.GuildId?.ToString() ?? "";
        if (interaction is SocketInteraction socketInteraction)
        {
            if (socketInteraction.Channel != null)
            {
                extra["channel.name"] = socketInteraction.Channel.Name;
            }
            if (socketInteraction.InteractionChannel is IGuildChannel guildChannel
                && guildChannel.Guild != null)
            {
                extra["guild.id"] = guildChannel.Guild.Id.ToString();
                extra["guild.name"] = guildChannel.Guild.Name;
                extra["guild.owner.id"] = guildChannel.Guild.OwnerId.ToString();
                try
                {
                    var owner = guildChannel.Guild.GetOwnerAsync().GetAwaiter().GetResult();
                    extra["guild.owner.username"] = owner?.Username ?? "";
                    extra["guild.owner.global_name"] = owner?.GlobalName ?? "";
                }
                catch { }
            }
            tags["author.id"] = socketInteraction.User.Id.ToString();
            tags["author.username"] = socketInteraction.User.Username;
            tags["author.global_name"] = socketInteraction.User.GlobalName;
        }

        scope.SetTags(tags);
        scope.SetExtras(extra);
    }

    public static void SetInteractionInfo(this Scope scope, IInteractionContext? context)
    {
        if (context == null || context.Interaction == null)
            return;

        scope.SetInteractionInfo(context.Interaction);
        var tags = new Dictionary<string, string>();
        var extra = new Dictionary<string, object?>();
        
        if (context.Guild != null)
        {
            extra["guild.name"] = context.Guild.Name;
            extra["guild.owner.id"] = context.Guild.OwnerId.ToString();
            try
            {
                var owner = context.Guild.GetOwnerAsync().GetAwaiter().GetResult();
                extra["guild.owner.username"] = owner?.Username ?? "";
                extra["guild.owner.global_name"] = owner?.GlobalName ?? "";
            }
            catch { }
        }

        scope.SetTags(tags);
        scope.SetExtras(extra);
    }
}
