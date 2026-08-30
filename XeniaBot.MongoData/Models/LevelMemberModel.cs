using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Models;

namespace XeniaBot.MongoData.Models;

public class LevelMemberModel : BaseModel
{
    public static string CollectionName => "levelSystem";
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public ulong Xp { get; set; }
    public long LastMessageTimestamp { get; set; }
    public ulong LastMessageId { get; set; }
    public ulong LastMessageChannelId { get; set; }

    public async Task<IMessage?> GetMessage(SocketGuild? guild)
    {
        var textchannel = ExceptionHelper.RetryOnTimedOut(() => guild?.GetTextChannel(LastMessageChannelId));
        IMessage? message = null;
        try
        {
            if (textchannel != null)
            {
                message = await textchannel.GetMessageAsync(LastMessageId);
            }

            if (message != null) return message;
        }
        catch { }
        try
        {
            var vcchannel = ExceptionHelper.RetryOnTimedOut(() => guild?.GetVoiceChannel(LastMessageChannelId));
            if (vcchannel != null)
            {
                message = await vcchannel.GetMessageAsync(LastMessageId);
            }
            if (message != null) return message;
        }
        catch
        {
        }
        try
        {
            var threadchannel = ExceptionHelper.RetryOnTimedOut(() => guild?.GetThreadChannel(LastMessageChannelId));
            if (threadchannel != null)
            {
                message = await threadchannel.GetMessageAsync(LastMessageId);
            }

            if (message != null) return message;
        }
        catch
        {
        }
        try
        {
            var stagechannel = ExceptionHelper.RetryOnTimedOut(() =>
                guild?.GetStageChannel(LastMessageChannelId));
            if (stagechannel != null)
            {
                message = await stagechannel.GetMessageAsync(LastMessageId);
            }
        }
        catch
        {
        }
        return message;
    }
    public Task<IMessage?> GetMessage(DiscordSocketClient client)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => client.GetGuild(GuildId));
        return GetMessage(guild);
    }
    public Task<IMessage?> GetMessage(DiscordShardedClient client)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => client.GetGuild(GuildId));
        return GetMessage(guild);
    }

    public LevelMemberModel()
    {
        UserId = 0;
        GuildId = 0;
        Xp = 0;
        LastMessageTimestamp = 0;
        LastMessageId = 0;
        LastMessageChannelId = 0;
    }
}
