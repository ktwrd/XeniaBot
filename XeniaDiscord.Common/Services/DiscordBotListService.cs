using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XeniaBot.Shared;

namespace XeniaDiscord.Common.Services;

[XeniaController]
public class DiscordBotListService : BaseService
{
    private readonly ConfigData _config;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly HttpClient _httpClient = new();
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public DiscordBotListService(IServiceProvider services) : base(services)
    {
        _config = services.GetRequiredService<ConfigData>();
        _client = services.GetRequiredService<DiscordSocketClient>();
        _interactionService = services.GetRequiredService<InteractionService>();

        isReady = _client.CurrentUser != null;

        if (services.GetRequiredService<ProgramDetails>().Platform == XeniaPlatform.Bot)
        {
            InitializeTimerThread();
        }
    }

    private bool isReady = false;
    public override Task OnReady()
    {
        isReady = true;
        return base.OnReady();
    }

    private void InitializeTimerThread()
    {
        timerThread = new(() =>
        {
            try
            {
                TimerThread().Wait();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unhandled exception in timer thread. Restarting!");
                try
                {
                    InitializeTimerThread();
                }
                catch (Exception exx)
                {
                    _log.Error(exx, $"Failed to call {nameof(InitializeTimerThread)} inside of {nameof(InitializeTimerThread)}!!! This is really bad!!!");
                }
            }
        })
        {
            Name = $"Xenia.{nameof(DiscordBotListService)}.{nameof(TimerThread)}"
        };
        timerThread.Start();
        _log.Info($"Started thread: {timerThread.Name} (id:{timerThread.ManagedThreadId})");
    }
    private Thread? timerThread = null;
    private async Task TimerThread()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var ourThreadId = timerThread?.ManagedThreadId;

        // runs every 30s
        while (ourThreadId == timerThread?.ManagedThreadId)
        {
            if (!isReady)
            {
                var ago = DateTimeOffset.UtcNow - startedAt;
                if (ago >= TimeSpan.FromMinutes(1))
                {
                    _log.Warn($"Discord not ready, even though this thread was created {ago.Humanize(4)} ago");
                }
                await Task.Delay(500);
                continue;
            }
            if (string.IsNullOrEmpty(_config.ApiKeys.DiscordBotList))
            {
                await Task.Delay(15000);
                continue;
            }

            try
            {
                await PostStatistics();
            }
            catch (Exception ex)
            {
                _log.Warn(ex, "Failed to send statistics");
            }

            IEnumerable<ApplicationCommandDto>? commands = null;
            try
            {
                commands = await FindCommands();
            }
            catch (Exception ex)
            {
                _log.Warn(ex, "Failed to find commands to send to discordbotlist.com");
            }

            if (commands != null)
            {
                try
                {
                    await PostCommands(commands);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, "Failed to send commands");
                }
            }

            _log.Trace("Waiting 30s...");
            await Task.Delay(30_000);
        }
    }

    private async Task PostStatistics()
    {
        if (string.IsNullOrEmpty(_config.ApiKeys.DiscordBotList)) return;

        var users = _client.GroupChannels
            .SelectMany(e => e.Users.Select(e => e.Id))
            .Concat(_client.Guilds.SelectMany(e => e.Users.Select(e => e.Id)))
            .ToHashSet();

        var dto = new DblStatisticsDto()
        {
            Users = users.Count,
            Guilds = _client.Guilds.Count
        };

        var botId = _client.CurrentUser.Id;
        var message = new HttpRequestMessage()
        {
            RequestUri = new Uri($"https://discordbotlist.com/api/v1/bots/{botId}/stats"),
            Method = HttpMethod.Post,
            Content = new StringContent(dto.JsonSerialize(), Encoding.UTF8, "application/json")
        };
        await SendAsync(message);
    }

    private async Task<IEnumerable<ApplicationCommandDto>> FindCommands()
    {
        var dblCommands = _interactionService.SlashCommands.Where(command => command.Attributes.Any(e => e is RegisterDBLCommandAttribute)).ToList();
        var commands = await _client.GetGlobalApplicationCommandsAsync();

        var mapped = new List<ApplicationCommandDto>();
        foreach (var command in commands)
        {
            var commandNames = new List<string>();
            var options = command.Options.Where(e => e.Type == ApplicationCommandOptionType.SubCommand).ToArray();
            if (options.Length < 1) commandNames.Add(command.Name);
            else commandNames.AddRange(options.Select(opt => $"{command.Name} {opt.Name}").Distinct());

            if (commandNames.Any(ShouldIncludeCommand))
            {
                mapped.Add(ApplicationCommandDto.Convert(command));
            }
        }
        return mapped;

        bool ShouldIncludeCommand(string commandName)
        {
            foreach (var command in dblCommands)
            {
                var formattedCommandName = command.Name;
                if (!string.IsNullOrEmpty(command.Module.SlashGroupName))
                {
                    formattedCommandName = $"{command.Module.SlashGroupName} {formattedCommandName}";
                }
                if (formattedCommandName == commandName) return true;
            }
            return false;
        }
    }

    private async Task PostCommands(IEnumerable<ApplicationCommandDto> commands)
    {
        if (string.IsNullOrEmpty(_config.ApiKeys.DiscordBotList)) return;

        var json = JsonSerializer.Serialize(commands, serializerOptions)
            ?? throw new InvalidOperationException($"Failed to convert commands to JSON (result is null)");

        var botId = _client.CurrentUser.Id;
        var message = new HttpRequestMessage()
        {
            RequestUri = new Uri($"https://discordbotlist.com/api/v1/bots/{botId}/commands"),
            Method = HttpMethod.Post,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        await SendAsync(message);
    }

    private readonly static JsonSerializerOptions serializerOptions = new()
    {
        IncludeFields = true,
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private async Task<(HttpResponseMessage Response, string ResponseBody)> SendAsync(HttpRequestMessage message)
    {
        message.Headers.Add("Authorization", "Bot " + _config.ApiKeys.DiscordBotList);
        var response = await _httpClient.SendAsync(message);
        var responseBody = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode)
        {
            _log.Error($"Failed to send statistics to {message.RequestUri} ({(int)response.StatusCode}, {response.StatusCode})\n{responseBody}");
        }
        return (response, responseBody);
    }
}

public class DblStatisticsDto
{
    [JsonPropertyName("voice_connections")]
    public long VoiceConnections { get; set; } = 0;

    [JsonPropertyName("users")]
    public long Users { get; set; }

    [JsonPropertyName("guilds")]
    public long Guilds { get; set; }
    
    [JsonPropertyName("shard_id")]
    public int ShardId { get; set; }

    public string JsonSerialize()
    {
        return JsonSerializer.Serialize(this, SerializerOptions) ?? throw new InvalidOperationException("Somehow failed to convert this instance to a JSON??");
    }
    
    private readonly static JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };
}

public class ApplicationCommandDto
{
    [JsonRequired]
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonRequired]
    [JsonPropertyName("type")]
    public ApplicationCommandType Type { get; set; } = ApplicationCommandType.Slash; // defaults to 1 which is slash.

    [JsonRequired]
    [JsonPropertyName("application_id")]
    public ulong ApplicationId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? GuildId { get; set; }

    [JsonRequired]
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonRequired]
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApplicationCommandOptionDto[]? Options { get; set; }

    [JsonPropertyName("default_permission")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DefaultPermissions { get; set; }

    [JsonPropertyName("name_localizations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? NameLocalizations { get; set; }

    [JsonPropertyName("description_localizations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? DescriptionLocalizations { get; set; }

    [JsonPropertyName("name_localized")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NameLocalized { get; set; }

    [JsonPropertyName("description_localized")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DescriptionLocalized { get; set; }

    // V2 Permissions
    [JsonPropertyName("dm_permission")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DmPermission { get; set; }

    [JsonPropertyName("default_member_permissions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GuildPermission? DefaultMemberPermission { get; set; }

    [JsonPropertyName("nsfw")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Nsfw { get; set; }

    [JsonPropertyName("contexts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InteractionContextType[]? ContextTypes { get; set; }

    [JsonPropertyName("integration_types")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApplicationIntegrationType[]? IntegrationTypes { get; set; }

    public static ApplicationCommandDto Convert(SocketApplicationCommand command)
    {
        var instance = new ApplicationCommandDto()
        {
            Type = command.Type,
            ApplicationId = command.ApplicationId,
            Name = command.Name,
            Description = command.Description,
            DefaultPermissions = command.IsDefaultPermission,
            NameLocalizations = command.NameLocalizations == null || command.NameLocalizations.Count < 1 ? null : new Dictionary<string, string>(command.NameLocalizations),
            DescriptionLocalizations = command.DescriptionLocalizations == null || command.DescriptionLocalizations.Count < 1 ? null : new Dictionary<string, string>(command.DescriptionLocalizations),
            NameLocalized = command.NameLocalized,
            DescriptionLocalized = command.DescriptionLocalized,
            DmPermission = command.IsEnabledInDm,
            DefaultMemberPermission = (GuildPermission)command.DefaultMemberPermissions.RawValue,
            Nsfw = command.IsNsfw,
            ContextTypes = command.ContextTypes == null || command.ContextTypes.Count < 1 ? null : command.ContextTypes.ToArray(),
            IntegrationTypes = command.IntegrationTypes == null || command.IntegrationTypes.Count < 1 ? null : command.IntegrationTypes.ToArray(),
        };
        if (command.Options != null)
        {
            var options = new List<ApplicationCommandOptionDto>();

            foreach (var option in command.Options)
            {
                options.Add(ApplicationCommandOptionDto.Convert(option));
            }

            instance.Options = options.ToArray();
        }

        return instance;

    }
}
public class ApplicationCommandOptionDto
{
    public static ApplicationCommandOptionDto Convert(SocketApplicationCommandOption option)
    {
        var instance = new ApplicationCommandOptionDto()
        {
            Type = option.Type,
            Name = option.Name,
            Description = option.Description,
            Default = option.IsDefault,
            Required = option.IsRequired,
            Autocomplete = option.IsAutocomplete,
            MinValue = option.MinValue,
            MaxValue = option.MaxValue,
            ChannelTypes = option.ChannelTypes == null || option.ChannelTypes.Count < 1 ? null : option.ChannelTypes.ToArray(),
            NameLocalizations = option.NameLocalizations == null || option.NameLocalizations.Count < 1 ? null : new Dictionary<string, string>(option.NameLocalizations),
            DescriptionLocalizations = option.DescriptionLocalizations == null || option.DescriptionLocalizations.Count < 1 ? null : new Dictionary<string, string>(option.DescriptionLocalizations),
            NameLocalized = option.NameLocalized,
            DescriptionLocalized = option.DescriptionLocalized,
            MinLength = option.MinLength,
            MaxLength = option.MaxLength
        };
        if (option.Options != null && option.Options.Count > 0)
        {
            var options = new List<ApplicationCommandOptionDto>();
            foreach (var item in option.Options)
            {
                options.Add(Convert(item));
            }
            instance.Options = options.ToArray();
        }
        if (option.Choices != null && option.Choices.Count > 0)
        {
            var choices = new List<ApplicationCommandOptionChoiceDto>();
            foreach (var item in option.Choices)
            {
                choices.Add(ApplicationCommandOptionChoiceDto.Convert(item));
            }
            instance.Choices = choices.ToArray();
        }

        return instance;
    }

    [JsonRequired]
    [JsonPropertyName("type")]
    public ApplicationCommandOptionType Type { get; set; }

    [JsonRequired]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonRequired]
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("default")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Default { get; set; }

    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Required { get; set; }

    [JsonPropertyName("choices")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApplicationCommandOptionChoiceDto[]? Choices { get; set; }

    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApplicationCommandOptionDto[]? Options { get; set; }

    [JsonPropertyName("autocomplete")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Autocomplete { get; set; }

    [JsonPropertyName("min_value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? MinValue { get; set; }

    [JsonPropertyName("max_value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? MaxValue { get; set; }

    [JsonPropertyName("channel_types")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChannelType[]? ChannelTypes { get; set; }

    [JsonPropertyName("name_localizations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? NameLocalizations { get; set; }

    [JsonPropertyName("description_localizations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? DescriptionLocalizations { get; set; }

    [JsonPropertyName("name_localized")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NameLocalized { get; set; }

    [JsonPropertyName("description_localized")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DescriptionLocalized { get; set; }

    [JsonPropertyName("min_length")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinLength { get; set; }

    [JsonPropertyName("max_length")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxLength { get; set; }
}
public class ApplicationCommandOptionChoiceDto
{
    public static ApplicationCommandOptionChoiceDto Convert(SocketApplicationCommandChoice choice)
    {
        return new ApplicationCommandOptionChoiceDto
        {
            Name = choice.Name,
            Value = choice.Value,
            NameLocalizations = choice.NameLocalizations == null || choice.NameLocalizations.Count < 1
                ? null
                : new Dictionary<string, string>(choice.NameLocalizations),
            NameLocalized = choice.NameLocalized
        };
    }

    [JsonRequired]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonRequired]
    [JsonPropertyName("value")]
    public required object Value { get; set; }

    [JsonPropertyName("name_localizations")]
    public Dictionary<string, string>? NameLocalizations { get; set; }

    [JsonPropertyName("name_localized")]
    public string? NameLocalized { get; set; }
}