using MongoDB.Bson;
using System.ComponentModel;

namespace XeniaBot.Data.Models;

public class ConfigGuildTicketModel
{
    [Browsable(false)]
    public ObjectId _id { get; set; }
    public ulong GuildId { get; set; }
    public ulong CategoryId { get; set; }
    public ulong RoleId { get; set; }
    public ulong LogChannelId { get; set; }
}
