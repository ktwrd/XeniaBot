using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Discord;
using Discord.WebSocket;
using XeniaBot.Shared.Services;

namespace XeniaBot.Shared.Helpers;

public static class XeniaHelper
{
    public static EmbedBuilder BaseEmbed(EmbedBuilder? builder = null)
    {
        if (CoreContext.Instance == null)
            throw new Exception("CoreContext hasn't been initialized.");

        var client = CoreContext.Instance.GetRequiredService<DiscordSocketClient>();
        return BaseEmbed(client, builder);
    }

    /// <summary>
    /// Format a Start and End timestamp into a string.
    /// </summary>
    /// <returns>HH hour(s) MM minute(s) SS second(s)</returns>
    public static string FormatDuration(DateTimeOffset start, DateTimeOffset end)
    {
        var result = new List<string>();
        var span = end - start;

        string pluralize(int c)
        {
            return c > 1 ? "s" : "";
        }

        if (span.TotalSeconds < 1)
        {
            return $"{span.TotalMilliseconds}ms";
        }
        
        if (span.Hours > 0)
            result.Add($"{span.Hours} hour" + pluralize(span.Hours));
        if (span.Minutes > 0)
            result.Add($"{span.Minutes} minute" + pluralize(span.Minutes));
        if (span.Seconds > 0)
            result.Add($"{span.Seconds} second" + pluralize(span.Seconds));
        return string.Join(" ", result);
    }
    /// <summary>
    /// <inheritdoc cref="FormatDuration(System.DateTimeOffset,System.DateTimeOffset)"/>
    ///
    /// <para>Assumes that <paramref name="start"/> was created with <see cref="DateTimeOffset.UtcNow"/></para>
    /// </summary>
    public static string FormatDuration(DateTimeOffset start)
    {
        return FormatDuration(start, DateTimeOffset.UtcNow);
    }
    
    public static bool ChannelExists(DiscordSocketClient client, ulong guildId, ulong channelId)
    {
        try
        {
            var guild = client.GetGuild(guildId);
            var channel = guild.GetChannel(channelId);
            return channel != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool ChannelExists(IGuild guild, ulong channelId)
    {
        try
        {
            var channel = guild.GetChannelAsync(channelId).Result;
            return channel != null;
        }
        catch
        {
            return false;
        }
    }
    public static EmbedBuilder BaseEmbed(DiscordSocketClient client, EmbedBuilder? embed=null)
    {
        embed ??= new EmbedBuilder();

        var icon = client.CurrentUser.GetAvatarUrl();

        string? version = null;
        if (CoreContext.Instance != null && CoreContext.Instance?.Details.Version != null)
            version = CoreContext.Instance?.Details.Version;

        var footer = new EmbedFooterBuilder()
            .WithIconUrl(icon);
        if (version != null)
            footer.WithText($"Xenia v{version}");
        
        return embed
            .WithTimestamp(DateTimeOffset.UtcNow)
            .WithFooter(footer);
    }
    
    public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
    {
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve
    };

    public static string Pluralize(int count)
    {
        return count > 0 ? "s" : "";
    }

    /// <summary>
    /// Converts "PascalCase" to "Pascal Case"
    /// </summary>
    /// <param name="input">String to format</param>
    /// <returns>Formatted result</returns>
    public static string FormatPascalCase(string input)
    {
        string result = "";
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            string cs = input[i].ToString();
            if (cs.ToUpper() == cs && i != 0)
                result += $" {c}";
            else
                result += c;
        }
        return result;
    }
    public static string GetGuildPrefix(ulong guildId, ConfigData data)
    {
        return data.Prefix;
    }
    public static string[] GenerateDifference(string before, string after)
    {
        if (before == null)
            before = "";
        if (after == null)
            after = "";
        var diff = InlineDiffBuilder.Diff(before, after);
        var lines = new List<string>();
        foreach (var line in diff.Lines)
        {
            var lineContent = "";
            if (line.Type == ChangeType.Inserted)
                lineContent += "+ ";
            else if (line.Type == ChangeType.Deleted)
                lineContent += "- ";
            else if (line.Type == ChangeType.Modified)
                lineContent += "M ";
            else if (line.Type == ChangeType.Imaginary)
                lineContent += "I ";
            else
                lineContent += "  ";
            lineContent += line.Text;
            lines.Add(lineContent);
        }

        return lines.ToArray();
    }

    public static async Task TaskWhenAll(ICollection<Task> tasks, bool startAll = true)
    {
        if (startAll)
        {
            foreach (var i in tasks)
            {
                i.Start();
            }
        }
        await Task.WhenAll(tasks);
    }

    public static string ToHex(Discord.Color color)
    {
        var s = "";
        s += color.R.ToString("X2");
        s += color.G.ToString("X2");
        s += color.B.ToString("X2");
        return s;
    }

    public static Discord.Color FromHex(string hex)
    {
        var str = "";
        if (!hex.StartsWith("#"))
            str += "#";
        str += hex;
        var color = System.Drawing.ColorTranslator.FromHtml(str);
        return new Discord.Color(color.R, color.G, color.B);
    }

    public static Dictionary<string, object?>? ReflectionToDictionary(object? obj, out List<string> skippedProperties)
    {
        skippedProperties = new();
        if (obj == null)
            return null;
        var dict = new Dictionary<string, object?>();
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var allowedTypes = new List<Type>()
        {
            typeof(string),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(byte),
            typeof(char)
        };
        foreach (var x in properties)
        {
            bool found = false;
            foreach (var it in allowedTypes)
            {
                if (it.IsAssignableFrom(x.PropertyType))
                {
                    // only allow enums to be casted into non-strings.
                    if (x.PropertyType.IsEnum &&
                        (typeof(string).IsAssignableFrom(x.PropertyType) ||
                         typeof(char).IsAssignableFrom(x.PropertyType)))
                        continue;
                    dict[x.Name] = x.GetValue(obj)?.ToString();
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                skippedProperties.Add(x.Name);
            }
        }

        return dict;
    }
    public static Dictionary<string, object?>? DictionarySerialize(this SocketMessage? message)
    {
        var dict = ReflectionToDictionary(message, out var skippedProperties);
        if (dict == null || message == null)
            return null;
        var properties = message.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var x in properties.Where(v => skippedProperties.Contains(v.Name)))
        {
            if (typeof(IGuild).IsAssignableFrom(x.PropertyType))
            {
                var obj = (IGuild?)x.GetValue(message);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
            else if (typeof(ICategoryChannel).IsAssignableFrom(x.PropertyType))
            {
                var obj = (ICategoryChannel?)x.GetValue(message);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
            else if (typeof(IEmote).IsAssignableFrom(x.PropertyType))
            {
                var obj = (IEmote?)x.GetValue(message);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
        }

        return dict;
    }

    public static Dictionary<string, object?>? DictionarySerialize(this ISocketMessageChannel? channel)
    {
        var dict = ReflectionToDictionary(channel, out var skippedProperties);
        if (dict == null || channel == null)
            return null;
        var properties = channel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var x in properties.Where(v => skippedProperties.Contains(v.Name)))
        {
            if (typeof(IGuild).IsAssignableFrom(x.PropertyType))
            {
                var obj = (IGuild?)x.GetValue(channel);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
            else if (typeof(ICategoryChannel).IsAssignableFrom(x.PropertyType))
            {
                var obj = (ICategoryChannel?)x.GetValue(channel);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
            else if (typeof(IEmote).IsAssignableFrom(x.PropertyType))
            {
                var obj = (IEmote?)x.GetValue(channel);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
        }

        return dict;
    }

    public static Dictionary<string, object?>? DictionarySerialize(this SocketForumChannel? channel)
    {
        var dict = ReflectionToDictionary(channel, out var skippedProperties);
        if (dict == null || channel == null)
            return null;
        var properties = channel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var x in properties.Where(v => skippedProperties.Contains(v.Name)))
        {
            if (typeof(IGuild).IsAssignableFrom(x.PropertyType))
            {
                var obj = (IGuild?)x.GetValue(channel);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
            else if (typeof(ICategoryChannel).IsAssignableFrom(x.PropertyType))
            {
                var obj = (ICategoryChannel?)x.GetValue(channel);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
            else if (typeof(IEmote).IsAssignableFrom(x.PropertyType))
            {
                var obj = (IEmote?)x.GetValue(channel);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
        }
        return dict;
    }

    public static Dictionary<string, object?>? DictionarySerialize(this IGuild? guild)
    {
        var dict = ReflectionToDictionary(guild, out var skippedProperties);
        if (dict == null || guild == null)
            return null;
        var properties = guild.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var x in properties.Where(v => skippedProperties.Contains(v.Name)))
        {if (typeof(ICategoryChannel).IsAssignableFrom(x.PropertyType))
            {
                var obj = (ICategoryChannel?)x.GetValue(guild);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
            else if (typeof(IEmote).IsAssignableFrom(x.PropertyType))
            {
                var obj = (IEmote?)x.GetValue(guild);
                dict[x.Name] = ReflectionToDictionary(obj, out var _);
            }
            else if (typeof(ReadOnlyCollection<SocketCustomSticker>).IsAssignableFrom(x.PropertyType) || typeof(IEnumerable<SocketCustomSticker>).IsAssignableFrom(x.PropertyType))
            {
                var obj = (IEnumerable<SocketCustomSticker>?)x.GetValue(guild);
                dict[x.Name] = obj.DictionarySerialize();
            }
        }
        return dict;
    }

    public static List<Dictionary<string, object?>>? DictionarySerialize(
        this IEnumerable<ICustomSticker>? stickers)
    {
        if (stickers == null)
            return null;
        var result = new List<Dictionary<string, object?>>();
        foreach (var x in stickers)
        {
            var v = ReflectionToDictionary(x, out var _);
            if (v != null)
                result.Add(v);
        }

        return result;
    }
}