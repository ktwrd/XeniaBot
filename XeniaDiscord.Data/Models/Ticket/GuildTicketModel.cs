using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Data.Models.Ticket;

public class GuildTicketModel
{
    public const string TableName = "GuildTicket";
    public GuildTicketModel()
    {
        Id = Guid.NewGuid();
        GuildId = "0";
        ChannelId = "0";
        CreatedAt = DateTimeOffset.UtcNow;
        Status = GuildTicketStatus.Unknown;
        Users = [];
    }

    public Guid Id { get; set; }
    public string GuildId { get; set; }
    public string ChannelId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public GuildTicketStatus Status { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? ClosedByUserId { get; set; }

    // property accessor
    public List<GuildTicketUserModel> Users { get; set; }

    public ulong GetGuildId()
    {
        if (ulong.TryParse(GuildId, out var r)) return r;
        return 0;
    }
    public ulong GetChannelId()
    {
        if (ulong.TryParse(ChannelId, out var r)) return r;
        return 0;
    }
    public GuildTicketModel Clone()
    {
        if (this.MemberwiseClone() is GuildTicketModel r) return r;
        throw new NotImplementedException();
    }
}

public enum GuildTicketStatus : byte
{
    Unknown = 0,
    Open,
    Resolved,
    Rejected
}