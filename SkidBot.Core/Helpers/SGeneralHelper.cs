using System.Collections.Generic;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace SkidBot.Core.Helpers;

public static class SGeneralHelper
{
    public static string[] GenerateDifference(string before, string after)
    {
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
}