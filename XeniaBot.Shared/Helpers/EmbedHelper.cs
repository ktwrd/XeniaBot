using CSharpFunctionalExtensions;
using System.Text;

namespace XeniaBot.Shared.Helpers;

public static class EmbedHelper
{
    public static bool FieldRequiresAttachment(string content)
        => FieldRequiresAttachment(content, Maybe<string>.None);
    public static bool FieldRequiresAttachment(
        string content,
        Maybe<string> codeBlock)
    {
        if (codeBlock.HasNoValue) return content.Length >= 1024;

        var sb = new StringBuilder("```");
        if (!string.IsNullOrEmpty(codeBlock.Value))
            sb.Append(codeBlock.Value);
        sb.AppendLine();
        sb.Append(content);
        sb.AppendLine();
        sb.Append("```");
        return sb.Length >= 1024;
    }
}
