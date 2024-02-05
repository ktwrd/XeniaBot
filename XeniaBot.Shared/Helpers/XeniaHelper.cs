using System;
using System.Collections.Generic;
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
}