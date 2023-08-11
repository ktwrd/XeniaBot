using System.Text.Json;
using Discord.WebSocket;
using XeniaBot.Core.Controllers.Wrappers.BigBrother;

namespace XeniaBot.Core.Helpers;

public static class BigBrotherHelper
{
    public static TH? ForceTypeCast<T, TH>(T input)
    {
        var options = new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true
        };
        var text = JsonSerializer.Serialize(input, options);
        var output = JsonSerializer.Deserialize<TH>(text, options);
        return output;
    }

    public static BB_ChannelType GetChannelType<T>(T channel) where T : SocketChannel
    {
        if (channel is SocketCategoryChannel)
            return BB_ChannelType.Category;
        else if (channel is SocketGroupChannel)
            return BB_ChannelType.Group;
        else if (channel is SocketDMChannel)
            return BB_ChannelType.DM;
        else if (channel is SocketForumChannel)
            return BB_ChannelType.Forum;
        else if (channel is SocketNewsChannel)
            return BB_ChannelType.News;
        else if (channel is SocketStageChannel)
            return BB_ChannelType.Stage;
        else if (channel is SocketThreadChannel)
            return BB_ChannelType.Thread;
        else if (channel is SocketVoiceChannel)
            return BB_ChannelType.Voice;
        else if (channel is SocketTextChannel)
            return BB_ChannelType.Text;
        else
            return BB_ChannelType.Unknown;
    }
}