using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Core.Models
{
    public class LevelMemberModel : BaseModel
    {
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public ulong Xp { get; set; }
        public long LastMessageTimestamp { get; set; }
        public ulong LastMessageId { get; set; }
        public ulong LastMessageChannelId { get; set; }
        public async Task<IMessage?> GetMessage(DiscordSocketClient client)
        {
            var guild = client.GetGuild(GuildId);
            
            var textchannel = guild?.GetTextChannel(LastMessageChannelId);
            IMessage? message = null;
            try
            {
                if (textchannel != null)
                {
                    message = await textchannel.GetMessageAsync(LastMessageId);
                }
                else
                {
                    try
                    {
                        var vcchannel = guild?.GetVoiceChannel(LastMessageChannelId);
                        if (vcchannel != null)
                        {
                            message = await vcchannel.GetMessageAsync(LastMessageId);
                        }
                        else
                        {
                            try
                            {
                                var threadchannel = guild?.GetThreadChannel(LastMessageChannelId);
                                if (threadchannel != null)
                                {
                                    message = await threadchannel.GetMessageAsync(LastMessageId);
                                }
                                else
                                {
                                    try
                                    {
                                        var stagechannel = guild?.GetStageChannel(LastMessageChannelId);
                                        if (stagechannel != null)
                                        {
                                            message = await stagechannel.GetMessageAsync(LastMessageId);
                                        }
                                    }
                                    catch { }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch{ }
            return message;
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
}
