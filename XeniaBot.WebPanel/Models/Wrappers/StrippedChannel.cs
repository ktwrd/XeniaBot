using System.Collections.Generic;
using System.Linq;
using Discord;

namespace XeniaBot.WebPanel.Models;

public class StrippedChannel
{
    public required ulong Id { get; set; }
    public required string Name { get; set; }
    public required int Position { get; set; }

    public static IEnumerable<StrippedChannel> FromGuild(IGuild guild)
    {
        return guild.GetChannelsAsync().GetAwaiter().GetResult().Select((v) =>
        {
            return new StrippedChannel()
            {
                Id = v.Id,
                Name = v.Name,
                Position = v.Position
            };
        });
    }
}