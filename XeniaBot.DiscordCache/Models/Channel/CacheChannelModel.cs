using XeniaBot.DiscordCache.Helpers;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheChannelModel : DiscordCacheBaseModel
{
    public CacheChannelType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public CacheDmChannelModel? DMChannel { get; set; }
    public CacheGuildChannelModel? GuildChannel { get; set; }
    public CacheForumChannelModel? ForumChannel { get; set; }
    public CacheGroupChannelModel? GroupChannel { get; set; }
    public CacheTextChannelModel? TextChannel { get; set; }
    public CacheTextChannelModel? NewsChannel { get; set; }
    public CacheThreadChannelModel? ThreadChannel { get; set; }
    public CacheVoiceChannelModel? VoiceChannel { get; set; }
    public CacheStageChannelModel? StageChannel { get; set; }
    public void Generate(SocketChannel channel)
    {
        Type = DiscordCacheHelper.GetChannelType(channel);
        Snowflake = channel.Id;
        CreatedAt = channel.CreatedAt;
        if (channel is SocketDMChannel dmChannel)
            DMChannel = CacheDmChannelModel.FromExisting(dmChannel);
        if (channel is SocketForumChannel forumChannel)
            ForumChannel = CacheForumChannelModel.FromExisting(forumChannel);
        if (channel is SocketGroupChannel groupChannel)
            GroupChannel = CacheGroupChannelModel.FromExisting(groupChannel);

        if (channel is SocketStageChannel stageChannel)
            StageChannel =  CacheStageChannelModel.FromExisting(stageChannel);
        if (channel is SocketVoiceChannel voiceChannel)
            VoiceChannel =  CacheVoiceChannelModel.FromExisting(voiceChannel);
        if (channel is SocketNewsChannel newsChannel)
            NewsChannel =  CacheTextChannelModel.FromExisting(newsChannel);
        if (channel is SocketThreadChannel threadChannel)
            ThreadChannel =  CacheThreadChannelModel.FromExisting(threadChannel);
        if (channel is SocketTextChannel textChannel)
            TextChannel =  CacheTextChannelModel.FromExisting(textChannel);

        
        if (channel is SocketGuildChannel guildChannel)
            GuildChannel = CacheGuildChannelModel.FromExisting(guildChannel);
    }
}