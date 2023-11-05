using Discord;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheGuildChannelModel : CacheBaseChannel
{
    public CacheGuildModel Guild { get; set; }
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public int Position { get; set; }
    public ChannelFlags Flags { get; set; }
    public CacheOverwrite[] PermissionOverwrites { get; set; }
    public CacheGuildChannelModel Update(SocketGuildChannel channel)
    {
        base.Update(channel);
        Guild = new CacheGuildModel().FromExisting(channel.Guild);
        GuildId = channel.Guild.Id;
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

    public static CacheGuildChannelModel? FromExisting(SocketGuildChannel? channel)
    {
        if (channel == null)
            return null;

        var instance = new CacheGuildChannelModel();
        return instance.Update(channel);
    }
}