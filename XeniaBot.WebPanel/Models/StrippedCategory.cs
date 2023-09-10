using Discord.WebSocket;

namespace XeniaBot.WebPanel.Models;

public class StrippedCategory
{
    public IEnumerable<ulong> ChannelIds { get; set; }
    public string Name { get; set; }
    public int Position { get; set; }
    public ulong Id { get; set; }

    public static StrippedCategory FromCategory(SocketCategoryChannel category)
    {
        return new StrippedCategory()
        {
            ChannelIds = category.Channels.Select(v => v.Id),
            Name = category.Name,
            Position = category.Position,
            Id = category.Id
        };
    }
    public static IEnumerable<StrippedCategory> FromGuild(SocketGuild guild)
    {
        var items = new List<StrippedCategory>();
        foreach (var item in guild.CategoryChannels)
        {
            items.Add(FromCategory(item));
        }

        return items;
    }
}