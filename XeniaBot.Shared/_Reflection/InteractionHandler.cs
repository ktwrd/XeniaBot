using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Sentry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared;

public class InteractionHandler
{
    private static readonly Logger Log = LogManager.GetLogger("Xenia." + nameof(InteractionHandler));
    private readonly InteractionService _interactionService;
    private readonly DiscordSocketClient _client;
    private readonly CoreContext _coreContext;
    private readonly IServiceProvider _services;
    private readonly ConfigData _configData;
    public InteractionHandler(IServiceProvider services)
    {
        _interactionService = services.GetRequiredService<InteractionService>();
        _client = services.GetRequiredService<DiscordSocketClient>();
        _configData = services.GetRequiredService<ConfigData>();
        _coreContext = services.GetRequiredService<CoreContext>();
        _services = services;
    }

    public async Task InitializeAsync()
    {
        await _coreContext.RegisterModules(_interactionService, _services);
        var result = await _interactionService.RegisterCommandsGloballyAsync(deleteMissing: true);
        if (_coreContext.RegisterDeveloperModules != null)
        {
            var devModules = await _coreContext.RegisterDeveloperModules(_interactionService, _services);
            await _interactionService.AddModulesToGuildAsync(SharedGlobals.InternalGuildId, true, devModules);
        }
        
        var lines = new List<string>();
        foreach (var item in _interactionService.Modules)
        {
            int count = 0;
            count += item.AutocompleteCommands.Count;
            count += item.ComponentCommands.Count;
            count += item.ContextCommands.Count;
            count += item.ModalCommands.Count;
            count += item.SlashCommands.Count;
            lines.Add($"- {item.Name} ({count})");
        }
        Log.Debug($"Loaded [{_interactionService.Modules.Count}] modules\n" + string.Join("\n", lines));
        _client.InteractionCreated += InteractionCreateAsync;
        _client.ModalSubmitted += ModalSubmittedAsync;
        
        try
        {
            await SubmitDblCommands();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to submit commands to discordbotlist.com");
        }
    }

    private async Task ModalSubmittedAsync(SocketModal interaction)
    {
        try
        {
            var context = new SocketInteractionContext(
                _client,
                interaction);
            var result = await _interactionService.ExecuteCommandAsync(
                context,
                _services);
            // Debugger.Break();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to handle interation {interaction.Id} invoked by user \"{interaction.User.GlobalName}\" ({interaction.User.Username}, {interaction.User.Id})");
            SentrySdk.CaptureException(ex, scope =>
            {
                SentryHelper.SetInteractionInfo(scope, interaction);
            });
        }
    }

    private async Task InteractionCreateAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(
                _client,
                interaction);
            await _interactionService.ExecuteCommandAsync(
                context,
                _services);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to handle interation {interaction.Id} invoked by user \"{interaction.User.GlobalName}\" ({interaction.User.Username}, {interaction.User.Id})");
            SentrySdk.CaptureException(ex, scope =>
            {
                SentryHelper.SetInteractionInfo(scope, interaction);
            });
        }
    }

    private async Task SubmitDblCommands()
    {
        if (string.IsNullOrEmpty(_configData.ApiKeys.DiscordBotList)) return;

        var dblCommands = _interactionService.SlashCommands.Where(command => command.Attributes.Any(e => e is RegisterDBLCommandAttribute)).ToList();
        var commands = await _client.GetGlobalApplicationCommandsAsync();

        var mapped = new List<ApplicationCommandDto>();
        foreach (var command in commands)
        {
            var commandNames = new List<string>();
            var options = command.Options.Where(e => e.Type == ApplicationCommandOptionType.SubCommand).ToArray();
            if (options.Length < 1) commandNames.Add(command.Name);
            else
            {
                foreach (var opt in options)
                {
                    commandNames.Add($"{command.Name} {opt.Name}");
                }
            }

            if (commandNames.Any(ShouldIncludeCommand))
            {
                mapped.Add(ApplicationCommandDto.Convert(command));
            }
        }

        var request = new HttpRequestMessage()
        {
            RequestUri = new Uri($"https://discordbotlist.com/api/v1/bots/{_client.CurrentUser.Id}/commands"),
            Method = HttpMethod.Post,
        };
        request.Headers.Add("Authorization", $"Bot {_configData.ApiKeys.DiscordBotList}");
        var ser = JsonSerializer.Serialize(mapped, serializerOptions);
        request.Content = new StringContent(ser, null, "application/json");
        var client = new HttpClient();
        var res = await client.SendAsync(request);
        var responseBody = res.Content.ReadAsStringAsync().Result;
        if (res.IsSuccessStatusCode)
        {
            Log.Info($"Submitted {mapped.Count} commands to discordbotlist.com");
        }
        else
        {
            Log.Error($"Failed to submit command list to discordbotlist.com ({(int)res.StatusCode}\n{responseBody}");
        }

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
    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        IncludeFields = true,
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

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
                    options.Add(ApplicationCommandOptionDto.Convert(item));
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
        public object Value { get; set; }

        [JsonPropertyName("name_localizations")]
        public Dictionary<string, string>? NameLocalizations { get; set; }

        [JsonPropertyName("name_localized")]
        public string? NameLocalized { get; set; }
    }
}
