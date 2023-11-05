using Discord;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheGuildChannelModel : CacheBaseChannel
{
    public CacheGuildModel Guild { get; set; }
    public string Name { get; set; }
    public int Position { get; set; }
    public ChannelFlags Flags { get; set; }
    public CacheOverwrite[] PermissionOverwrites { get; set; }
    public CacheGuildChannelModel FromExisting(SocketGuildChannel channel)
    {
        base.FromExisting(channel);
        Guild = new CacheGuildModel().FromExisting(channel.Guild);
        Name = channel.Name;
        Position = channel.Position;
        Flags = channel.Flags;
        
        var overwriteList = new List<CacheOverwrite>();
        foreach (var item in channel.PermissionOverwrites)
        {
            overwriteList.Add(CacheOverwrite.FromExisting(item));
        }

        PermissionOverwrites = overwriteList.ToArray();
        
        IsGuildChannel = true;
        return this;
    }
}