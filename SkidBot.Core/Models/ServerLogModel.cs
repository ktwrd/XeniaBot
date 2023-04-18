namespace SkidBot.Core.Models;

public class ServerLogModel : BaseModel
{
    public ulong ServerId;
    public ulong DefaultLogChannel;
    public ulong? JoinChannel = null;
    public ulong? LeaveChannel = null;
}