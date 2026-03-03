using System;
using System.Text;

namespace XeniaBot.Shared;

public class ProgramDetails
{
    public XeniaPlatform Platform { get; init; }

    /// <summary>
    /// UTC of <see cref="DateTimeOffset.ToUnixTimeSeconds()"/>
    /// </summary>
    public long StartTimestamp { get; init; }

    /// <summary>
    /// Is Xenia running in debug mode?
    /// </summary>
    public bool Debug { get; init; }

    /// <summary>
    /// Can the user status be updated by this instance?
    /// </summary>
    public bool SetStatus { get; set; } = false;

    /// <summary>
    /// Unique tag identifiying the instance in a cluster.
    /// </summary>
    public string? PlatformTag { get; set; }

    /// <summary>
    /// Version w/o build time
    /// <code>
    /// version[-DEBUG]
    /// </code>
    /// </summary>
    public string Version
    {
        get
        {
            var result = VersionRawString ?? "unknown_version";
            if (Debug) result += "-DEBUG";
            return result;
        }
    }

    /// <summary>
    /// Full version
    /// <code>
    /// version[-DEBUG][ (yyyy/MM/dd HH:mm:ss)]
    /// </code>
    /// </summary>
    public string VersionFull
    {
        get
        {
            var sb = new StringBuilder(Version);
            if (VersionRaw?.Build > 365)
            {
                sb.AppendFormat(" ({0})", VersionDate.ToString("yyyy/MM/dd HH:mm:ss"));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Date when the application was compiled.
    /// </summary>
    public DateTime VersionDate
        => VersionDateEpoch
            .AddDays(VersionRaw?.Build ?? 0)
            .AddMinutes(VersionRaw?.Revision ?? 0);

    /// <summary>
    /// <see cref="VersionRaw"/> converted to a string
    /// </summary>
    public string? VersionRawString => VersionRaw?.ToString() ?? null;

    /// <summary>
    /// Underlying version from the Assembly
    /// </summary>
    public Version? VersionRaw { get; init; }

    /// <summary>
    /// Epoch for timestamped builds.
    /// </summary>
    public static readonly DateTime VersionDateEpoch = new(year: 2022, month: 1, day: 1, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Utc);
}
public enum XeniaPlatform
{
    Unknown = -1,
    WebPanel,
    Bot,
}