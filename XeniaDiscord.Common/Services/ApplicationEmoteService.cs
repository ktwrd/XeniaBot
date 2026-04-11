using System.ComponentModel;
using Discord;
using Discord.WebSocket;
using kate.shared.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Resources;

namespace XeniaDiscord.Common.Services;

/// <summary>
/// Used for managing: application emotes creation, and fetching application emotes
/// </summary>
public class ApplicationEmoteService : IXeniaOnReady
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ProgramDetails _details;
    private readonly DiscordSocketClient _client;

    public ApplicationEmoteService(IServiceProvider services)
    {
        _details = services.GetRequiredService<ProgramDetails>();
        _client = services.GetRequiredService<DiscordSocketClient>();
    }

    public Task OnReady()
    {
        if (_details.Platform != XeniaPlatform.Bot) return Task.CompletedTask;
        
        new Thread(() =>
        {
            try
            {
                OnReadyThread().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to run thread!");
            }
        })
        {
            Name = $"{nameof(ApplicationEmoteService)}.{nameof(OnReadyThread)}"
        }.Start();

        return Task.CompletedTask;
    }

    private IReadOnlyCollection<Emote> _applicationEmotes = [];

    public Emote? GetEmote(ApplicationEmote emote)
    {
        return _ready
            ? _applicationEmotes.FirstOrDefault(e => emote.ToDescriptionString() == e.Name)
            : null;
    }

    public Emote GetRequiredEmote(ApplicationEmote emote)
    {
        var emoteName = emote.ToDescriptionString();
        return _ready
            ? _applicationEmotes.FirstOrDefault(e => emote.ToDescriptionString() == e.Name)
                ?? throw new InvalidOperationException($"Could not find Application Emote: {emoteName}")
            : throw new InvalidOperationException("Emoji Service not ready!");
    }

    private bool _ready = false;
    private async Task OnReadyThread()
    {
        _log.Trace("Created thread");
        _applicationEmotes =  await ExceptionHelper.RetryOnTimedOut(async () => await _client.GetApplicationEmotesAsync());
        foreach (var (kind, resourceName) in InternalEmojiKeys)
        {
            var emojiName = kind.ToDescriptionString();
            if (string.IsNullOrEmpty(emojiName))
            {
                _log.Warn($"No emoji name (via DescriptionAttribute) set for {nameof(ApplicationEmote)}.{kind}");
                continue;
            }

            if (_applicationEmotes.Any(e => e.Name == kind.ToDescriptionString())) continue;

            _log.Info($"Uploading Application Emoji: {emojiName} (resourceName={resourceName})");
            using var stream = ResourceHelper.GetMemoryStream(resourceName, typeof(EmojiResources).Assembly);
            using var discordImage = new Image(stream);
            try
            {
                await ExceptionHelper.RetryOnTimedOut(async () => await _client.CreateApplicationEmoteAsync(emojiName, discordImage));
                _log.Info($"Created emoji: {emojiName}");
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to create emoji \"{emojiName}\" (resourceName={resourceName})");
            }
        }
        _applicationEmotes =  await ExceptionHelper.RetryOnTimedOut(async () => await _client.GetApplicationEmotesAsync());
        _ready = true;
    }

    private const string InternalEmojiNamespace = "XeniaDiscord.Resources.Emojis";
    private static IReadOnlyDictionary<ApplicationEmote, string> InternalEmojiKeys
        => new Dictionary<ApplicationEmote, string>()
        {
            { ApplicationEmote.GreenCheck, $"{InternalEmojiNamespace}.greencheck.png" },
            { ApplicationEmote.RedCross, $"{InternalEmojiNamespace}.redcross.png" },
            
            { ApplicationEmote.StatusDoNotDisturb, $"{InternalEmojiNamespace}.status-dnd.png" },
            { ApplicationEmote.StatusIdle, $"{InternalEmojiNamespace}.status-idle.png" },
            { ApplicationEmote.StatusOffline, $"{InternalEmojiNamespace}.status-offline.png" },
            { ApplicationEmote.StatusOnline, $"{InternalEmojiNamespace}.status-online.png" },
        }.AsReadOnly();

    public enum ApplicationEmote
    {
        [Description("greencheck")]
        GreenCheck,
        [Description("redcross")]
        RedCross,
        
        [Description("status_dnd")]
        StatusDoNotDisturb,
        [Description("status_idle")]
        StatusIdle,
        [Description("status_offline")]
        StatusOffline,
        [Description("status_online")]
        StatusOnline
    }
}