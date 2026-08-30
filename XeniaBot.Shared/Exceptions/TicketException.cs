using System;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace XeniaBot.Shared;

public class TicketException : Exception
{
    public TicketException(string? message) : base(message)
    { }
    public TicketException(string? message, Exception? innerException) : base(message, innerException)
    { }
}

public class TicketNotFoundException(string message) : TicketException(message)
{
    public ulong? TicketChannelId { get; init; }
}

public class TicketUserNotFoundException(string message) : TicketException(message)
{
    public ulong? TicketChannelId { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? UserId { get; init; }
}

public class TicketGuildNotConfiguredException(string message) : TicketException(message)
{
    public ulong? TicketChannelId { get; init; }
    public ulong? GuildId { get; init; }
}

public class TicketGuildNotFoundException(string message) : TicketException(message)
{
    public ulong? TicketChannelId { get; init; }
    public ulong? GuildId { get; init; }
}

public class TicketLogChannelNotFoundException(string message) : TicketException(message)
{
    public ulong? GuildId { get; init; }
    public required ulong LogChannelId { get; init; }
}
public class TicketChannelNotFoundException(string message) : TicketException(message)
{
    public required ulong TicketChannelId { get; init; }
    public ulong? GuildId { get; init; }
}

public class TicketUserNotParticipantException(string message) : TicketException(message)
{
    public required ulong TicketChannelId { get; init; }
    public required ulong UserId { get; init; }
    public ulong? GuildId { get; set; }
}

public class TicketManagerRoleNotFoundException(string message) : TicketException(message)
{
    public ulong? GuildId { get; init; }
    public ulong? RoleId { get; init; }
}