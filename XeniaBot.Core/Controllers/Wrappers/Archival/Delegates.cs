namespace XeniaBot.Core.Controllers.Wrappers.Archival;

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
    X_MessageModel current,
    X_MessageModel? previous);
    
public delegate void UserDiffDelegate(
    UserChangeType type,
    X_UserModel current,
    X_UserModel? previous);