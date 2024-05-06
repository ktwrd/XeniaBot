using System;
using System.Text;

namespace XeniaBot.Shared;

public enum XeniaPlatform
{
    WebPanel,
    Bot,
    StorageProxy
}
public class ProgramDetails
{
    public XeniaPlatform Platform { get; init; }
    /// <summary>
    /// UTC of <see cref="DateTimeOffset.ToUnixTimeSeconds()"/>
    /// </summary>
    public long StartTimestamp { get; init; }
    public bool Debug { get; init; }
    public string Version
    {
        get
        {
            string result = "";
            var targetAppend = VersionRawString;
            result += targetAppend ?? "null_version";
            if (Debug)
                result += "-DEBUG";
            return result;
        }
    }
    public string VersionFull
    {
        get
        {
            var sb = new StringBuilder();
            if (Debug)
                sb.Append($"{Version}-DEBUG");
            else
                sb.Append(Version);
            sb.Append($" ({VersionDate})");
            return sb.ToString();
        }
    }
    public DateTime VersionDate
    {
        get
        {
            DateTime buildDate = new DateTime(2000, 1, 1)
                .AddDays(VersionRaw?.Build ?? 0)
                .AddSeconds((VersionRaw?.Revision ?? 0) * 2);
            return buildDate;
        }
    }
    public string? VersionRawString
    {
        get
        {
            return VersionRaw?.ToString() ?? null;
        }
    }
    public Version? VersionRaw { get; init; }
}