using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Models.PartialSnapshot;

namespace XeniaDiscord.Common.Exceptions;

/// <summary>
/// Exception that is thrown by the BanSync service when there was a failure when adding a BanSyncRecord to the database.
/// </summary>
/// <remarks>
/// Generated with
/// <see href="https://ktwrd.github.io/csharp-exception-generator.html"/>
/// </remarks>
public class BanSyncDbAddRecordFailureException : Exception
{
    public BanSyncDbAddRecordFailureException(
        string message,
        Exception innerException,
        SocketUser socketUser,
        SocketGuild socketGuild,
        BanSyncRecordModel? bansyncRecord,
        UserPartialSnapshotModel? userPartialSnapshot)
        : base(message, innerException)
    {
        _message = message;
        User = socketUser;
        Guild = socketGuild;
        BanSyncRecord = bansyncRecord;
        UserPartialSnapshot = userPartialSnapshot;
    }

    public SocketUser User { get; }
    public SocketGuild Guild { get; }
    public BanSyncRecordModel? BanSyncRecord { get; }
    public UserPartialSnapshotModel? UserPartialSnapshot { get; }

    private readonly string _message;

    public override string Message
        => string.Join(Environment.NewLine,
        _message,
        $"------ {nameof(BanSyncDbAddRecordFailureException)} Details ------",
        $"User: {User} ({User.Id})",
        $"User Display Name: {User.GlobalName}",
        $"Guild ID: {Guild.Id}",
        $"Guild Name: {Guild.Name}",
        $"Guild Owner: {Guild.Owner} ({Guild.Owner.Id})",
        "",
        $"BanSyncRecord Id: {BanSyncRecord?.Id}",
        $"BanSyncRecord Reason: {BanSyncRecord?.Reason}",
        $"BanSyncRecord Source: {BanSyncRecord?.Source}",
        $"UserPartialSnapshot Id: {UserPartialSnapshot?.Id}",
        $"UserPartialSnapshot Timestamp: {UserPartialSnapshot?.Timestamp}");
}
