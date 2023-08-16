using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Discord;
using Discord.Rest;
using Newtonsoft.Json;

namespace XeniaBot.Shared;

public class DiscordApplicationCommand
  {
    [JsonPropertyName("version")]
    public ulong Version { get; set; }
    public DiscordApplicationCommand Cast(RestApplicationCommand cmd, Dictionary<string, string[]> blacklistSubCommands)
    {
      this.Id = cmd.Id;
      this.Version = ulong.Parse(new Random().NextInt64(0, long.MaxValue).ToString());
      this.Type = cmd.Type;
      this.ApplicationId = cmd.ApplicationId;
      this.Name = cmd.Name;
      this.Description = cmd.Description;
      this.DefaultPermissions = cmd.IsDefaultPermission;
      this.GuildId = null;

      this.Options = null;
      if (cmd.Options?.Count > 0)
      {
        this.Options = cmd.Options.Select(v => new DiscordCommandOption().Cast(v)).ToArray();
        string[] blacklistBansyncName = new string[]
        {
          "enableguild", "setguildstate"
        };
        if (blacklistSubCommands.TryGetValue(this.Name, out var blacklistArr))
          this.Options = this.Options.Where(v => !blacklistArr.Contains(v.Name)).ToArray();
      }

      this.DefaultMemberPermission = new GuildPermission?((GuildPermission)cmd.DefaultMemberPermissions.RawValue);
      this.DmPermission = cmd.IsEnabledInDm;
      this.Nsfw = cmd.IsNsfw;
      return this;
    }

    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("type")]
    public ApplicationCommandType Type { get; set; } = ApplicationCommandType.Slash;

    [JsonPropertyName("application_id")]
    public ulong ApplicationId { get; set; }

    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("options")]
    public DiscordCommandOption[]? Options { get; set; }

    [JsonPropertyName("default_permission")]
    public bool? DefaultPermissions { get; set; }

    [JsonPropertyName("dm_permission")]
    public bool? DmPermission { get; set; }

    [JsonPropertyName("default_member_permissions")]
    public GuildPermission? DefaultMemberPermission { get; set; }

    [JsonPropertyName("nsfw")]
    public bool? Nsfw { get; set; }
  }
  
  public class DiscordCommandOption
  {
    [JsonPropertyName("type")]
    public ApplicationCommandOptionType Type { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("default")]
    public bool? Default { get; set; }

    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    [JsonPropertyName("choices")]
    public DiscordCommandOptionChoice[]? Choices { get; set; }

    [JsonPropertyName("options")]
    public DiscordCommandOption[]? Options { get; set; }

    [JsonPropertyName("autocomplete")]
    public bool? Autocomplete { get; set; }

    [JsonPropertyName("min_value")]
    public double? MinValue { get; set; }

    [JsonPropertyName("max_value")]
    public double? MaxValue { get; set; }

    [JsonPropertyName("channel_types")]
    public ChannelType[]? ChannelTypes { get; set; }

    [JsonPropertyName("min_length")]
    public int? MinLength { get; set; }

    [JsonPropertyName("max_length")]
    public int? MaxLength { get; set; }

    public DiscordCommandOption Cast(RestApplicationCommandOption opt)
    {
      this.Type = opt.Type;
      this.Name = opt.Name;
      this.Description = opt.Description;
      
      this.Default = opt.IsDefault;
      
      this.Required = opt.IsRequired;
      
      this.MinValue = opt.MinValue;

      this.MaxValue = opt.MaxValue;

      this.Autocomplete = opt.IsAutocomplete;

      this.MinLength = opt.MinLength;
      this.MaxLength = opt.MaxLength;

      if (opt.Options != null && opt.Options?.Count > 0)
      {
        this.Options = opt.Options.Select(v => new DiscordCommandOption().Cast(v)).ToArray();
      }

      if (opt.Choices != null && opt.Choices?.Count > 0)
      {
        this.Choices = opt.Choices.Select(v => new DiscordCommandOptionChoice().Cast(v)).ToArray();
      }

      if (opt.ChannelTypes != null && opt.ChannelTypes.Count > 0)
      {
        this.ChannelTypes = opt.ChannelTypes.ToArray();
      }
      
      return this;
    }
  }
  public class DiscordCommandOptionChoice
  {
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public object Value { get; set; }

    public DiscordCommandOptionChoice Cast(RestApplicationCommandChoice choice)
    {
      this.Name = choice.Name;
      this.Value = choice.Value;
      return this;
    }
  }