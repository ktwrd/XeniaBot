using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Text;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Data.Models.ServerLog;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Common.Services;

[XeniaController]
public class ServerLogService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ServerLogRepository _serverLogRepo;
    private readonly DiscordSocketClient _discord;

    public ServerLogService(IServiceProvider services) : base(services)
    {
        _serverLogRepo = services.GetRequiredService<ServerLogRepository>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
    }

    #region Event Handle
    public Task EventHandle(ulong guildId, ServerLogEvent @event, EmbedBuilder embed, Dictionary<string, string>? attachments = null)
        => EventHandle(guildId, @event, [embed], attachments);

    public async Task EventHandle(ulong guildId, ServerLogEvent @event, EmbedBuilder[] embeds, Dictionary<string, string>? attachments = null)
    {
        var options = new EventHandleOptions(guildId, @event)
            .AddEmbeds(embeds)
            .AddAttachments(attachments?.Select(e => new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(e.Value)), e.Key)) ?? []);

        await EventHandle(options);
    }

    public async Task EventHandle(ulong guildId, ServerLogEvent @event, EmbedBuilder[] embeds, List<FileAttachment>? attachments = null)
    {
        var options = new EventHandleOptions(guildId, @event)
            .AddEmbeds(embeds)
            .AddAttachments(attachments ?? []);
        await EventHandle(options);
    }

    public async Task EventHandle(EventHandleOptions options)
    {
        var targetChannels = await _serverLogRepo.GetChannelsForGuild(options.GuildId, [options.Event, ServerLogEvent.Fallback]);
        var guild = _discord.GetGuild(options.GuildId);

        if (options.Attachments.Count > 10)
        {
            _log.Warn($"More than 10 attachments defined for event {options.Event} in guild \"{guild?.Name}\" (guildId={options.GuildId}, attachmentCount={options.Attachments.Count})");
        }

        if (guild == null) return;

        // do non-fallback events first, and skip fallback event if non-fallback events ran successfully
        var nonFallbackSent = false;
        foreach (var channel in targetChannels.OrderBy(e => e.Event == ServerLogEvent.Fallback ? 1 : 0))
        {
            try
            {
                if (channel.Event == ServerLogEvent.Fallback && nonFallbackSent)
                {
                    _log.Trace($"Non-fallback event(s) successfully ran. Skipping fallback channels. (GuildId={options.GuildId}, ChannelEvent={channel.Event}, Event={options.Event})");
                    continue;
                }

                var channelResult = await ProcessForModel(channel);
                if (channel.Event != ServerLogEvent.Fallback)
                {
                    nonFallbackSent |= channelResult;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to send event (GuildId={options.GuildId}, ChannelEvent={channel.Event}, Event={options.Event})");
            }
        }
        async Task<bool> ProcessForModel(ServerLogChannelModel channelModel)
        {
            var logChannel = await ExceptionHelper.RetryOnTimedOut(async () => guild.GetTextChannel(channelModel.GetChannelId()));
            if (logChannel == null) return false;

            return await ExceptionHelper.RetryOnTimedOut(async () => await EventHandleProcessInner(logChannel, options));
        }
    }
    private async Task<bool> EventHandleProcessInner(SocketTextChannel channel, EventHandleOptions options)
    {
        var guild = channel.Guild;
        try
        {
            if (options.Attachments.Count < 1)
            {
                await channel.SendMessageAsync(embeds: [.. options.Embeds.Select(e => e.Build())]);
                return true;
            }

            var attachmentList = options.Attachments.Count > 10
                ? options.Attachments.Take(10) : options.Attachments;
            if (options.Attachments.Count > 10)
            {
                _log.Warn($"More than 10 attachments defined when handling event {options.Event} in channel \"{channel.Name}\" in guild \"{guild.Name}\" (guildId={guild.Id}, channelId={channel.Id})");
            }

            await channel.SendFilesAsync(attachmentList, embeds: [.. options.Embeds.Select(e => e.Build())]);
            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to send message in channel \"{channel.Name}\" in guild \"{guild.Name}\" (guildId={guild.Id}, channelId={channel.Id})");

            if (ex.Message.Contains("Missing Access") || ex.Message.Contains("50001") || ex.Message.Contains("50013"))
            {
                try
                {
                    // make sure that formatting doesn't break
                    var guildNameEscaped = guild.Name.Replace("`", "").PadRight(1, ' ');
                    var channelName = channel.Name.Replace("`", "'").PadRight(1, ' ');

                    var channelUrl = $"https://discord.com/channels/{guild.Id}/{channel.Id}";
                    var channelNameInfo = $"It's the channel called `{channelName}`";
                    if (channel.Category != null)
                    {
                        var categoryNameFormatted = channel.Category.Name.Replace("`", "'");
                        channelNameInfo += $" in the category `{categoryNameFormatted}`";
                    }
                    await guild.Owner.SendMessageAsync(
                        string.Join(
                            "\n",
                            "Heya!",
                            "",
                            $"Xenia does not have access to send log events in a channel in the server {guildNameEscaped}, which you own.",
                            "",
                            "In order for the logging feature to work, make sure that Xenia has access to the following permissions.",
                            "- View Channel",
                            "- Send Messages",
                            "- Embed Links",
                            "- Attach Files",
                            "",
                            $"Channel affected: {channelUrl}",
                            $"-# {channelNameInfo}"
                        ));
                }
                catch (Exception exx)
                {
                    _log.Error(exx, $"Failed to DM owner \"{guild.Owner.Username}\" of guild \"{guild.Name}\" about not having the correct permissions in channel \"{channel.Name}\" (guildId={guild.Id}, ownerId={guild.OwnerId}, channelId={channel.Id})");
                }
            }
            else
            {
                throw;
            }
            return false;
        }
    }
    #endregion

    public class EventHandleOptions
    {
        public EventHandleOptions(ulong guildId, ServerLogEvent @event)
        {
            GuildId = guildId;
            Event = @event;
            Embeds = new List<EmbedBuilder>(4);
            Attachments = new List<FileAttachment>(10);
        }

        public ulong GuildId { get; }
        public ServerLogEvent Event { get; }
        public ICollection<EmbedBuilder> Embeds { get; }
        public ICollection<FileAttachment> Attachments { get; }

        public EventHandleOptions AddEmbeds(params IEnumerable<EmbedBuilder> embeds)
        {
            foreach (var embed in embeds) Embeds.Add(embed);
            return this;
        }

        public EventHandleOptions AddAttachments(params IEnumerable<FileAttachment> attachments)
        {
            foreach (var embed in attachments) Attachments.Add(embed);
            return this;
        }

        public EventHandleOptions AddAttachment(string filename, string content,
            string? description = null,
            bool spoiler = false)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var attachment = new FileAttachment(
                ms, filename,
                description: description,
                isSpoiler: spoiler);
            return AddAttachments(attachment);
        }
    }
}
