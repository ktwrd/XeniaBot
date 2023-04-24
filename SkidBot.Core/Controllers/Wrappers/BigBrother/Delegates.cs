namespace SkidBot.Core.Controllers.Wrappers.BigBrother;

public enum MessageChangeType
{
    Create,
    Delete,
    Update
}
public delegate void MessageDiffDelegate(
    MessageChangeType type,
    BB_MessageModel current,
    BB_MessageModel? previous);