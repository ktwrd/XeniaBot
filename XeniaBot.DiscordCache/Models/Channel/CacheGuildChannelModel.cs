using Discord;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheGuildChannelModel : CacheBaseChannel
{
    public CacheGuildModel Guild { get; set; }
    public string Name { get; set; }
    public int Position { get; set; }
    public ChannelFlags Flags { get; set; }
    public Overwrite[] PermissionOverwrite { get; set; }
    public CacheGuildChannelModel FromExisting(SocketGuildChannel channel)
    {
        base.FromExisting(channel);
        Guild = new CacheGuildModel().FromExisting(channel.Guild);
        Name = channel.Name;
        Position = channel.Position;
        Flags = channel.Flags;
        PermissionOverwrite = channel.PermissionOverwrites.ToArray();
        IsGuildChannel = true;
        return this;
    }
}