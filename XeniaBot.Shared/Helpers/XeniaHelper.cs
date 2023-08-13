﻿using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace XeniaBot.Shared.Helpers;

public static class XeniaHelper
{
    public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
    {
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = true,
        WriteIndented = true
    };

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

    public static async Task TaskWhenAll(IEnumerable<Task> tasks, bool startAll = true)
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
}