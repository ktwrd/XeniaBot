namespace SkidBot.Core.Controllers.Wrappers.BigBrother;

public enum MessageChangeType
{
    Create,
    Delete,
    Update
}

public enum UserChangeType
{
    Update,
    Delete
}
public delegate void MessageDiffDelegate(
    MessageChangeType type,
    BB_MessageModel current,
    BB_MessageModel? previous);
    
public delegate void UserDiffDelegate(
    UserChangeType type,
    BB_UserModel current,
    BB_UserModel? previous);