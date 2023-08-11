using System.Text.Json;
using Discord.WebSocket;
using XeniaBot.Core.Controllers.Wrappers.Archival;

namespace XeniaBot.Core.Helpers;

public static class ArchivalHelper
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

    public static XChannelType GetChannelType<T>(T channel) where T : SocketChannel
    {
        if (channel is SocketCategoryChannel)
            return XChannelType.Category;
        else if (channel is SocketGroupChannel)
            return XChannelType.Group;
        else if (channel is SocketDMChannel)
            return XChannelType.DM;
        else if (channel is SocketForumChannel)
            return XChannelType.Forum;
        else if (channel is SocketNewsChannel)
            return XChannelType.News;
        else if (channel is SocketStageChannel)
            return XChannelType.Stage;
        else if (channel is SocketThreadChannel)
            return XChannelType.Thread;
        else if (channel is SocketVoiceChannel)
            return XChannelType.Voice;
        else if (channel is SocketTextChannel)
            return XChannelType.Text;
        else
            return XChannelType.Unknown;
    }
}