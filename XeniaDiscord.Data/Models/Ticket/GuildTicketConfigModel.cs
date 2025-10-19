namespace XeniaDiscord.Data.Models.Ticket;

public class GuildTicketConfigModel
{
    public const string TableName = "GuildTicketConfig";

    public GuildTicketConfigModel()
    {
        Id = "0";
        CategoryId = "0";
        RoleId = "0";
        LogChannelId = "0";
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; set; }
    public string CategoryId { get; set; }
    public string RoleId { get; set; }
    public string LogChannelId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedByUserId { get; set; }

    public ulong GetGuildId()
    {
        if (ulong.TryParse(Id, out var r)) return r;
        return 0;
    }
    public ulong GetCategoryId()
    {
        if (ulong.TryParse(CategoryId, out var r)) return r;
        return 0;
    }
    public ulong GetRoleId()
    {
        if (ulong.TryParse(RoleId, out var r)) return r;
        return 0;
    }
    public ulong GetLogChannelId()
    {
        if (ulong.TryParse(LogChannelId, out var r)) return r;
        return 0;
    }
}
