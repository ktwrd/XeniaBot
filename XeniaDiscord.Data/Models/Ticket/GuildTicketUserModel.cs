namespace XeniaDiscord.Data.Models.Ticket;

public class GuildTicketUserModel
{
    public const string TableName = "GuildTicketUser";
    public GuildTicketUserModel()
    {
        UserId = "0";
    }

    public Guid TicketId { get; set; }
    public string UserId { get; set; }
    public ulong GetUserId()
    {
        if (ulong.TryParse(UserId, out var r)) return r;
        return 0;
    }
}
