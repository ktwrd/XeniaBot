using System.Collections.Generic;
using System.Threading.Tasks;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace XeniaBot.Core.Helpers;

public static class SGeneralHelper
{
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
}