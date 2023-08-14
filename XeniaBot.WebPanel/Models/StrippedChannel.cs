using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public class StrippedChannel
{
    public ulong Id { get; set; }
    public string Name { get; set; }

    public static IEnumerable<StrippedChannel> FromGuild(SocketGuild guild)
    {
        return guild.Channels.Select((v) =>
        {
            return new StrippedChannel()
            {
                Id = v.Id,
                Name = v.Name
            };
        });
    }
}