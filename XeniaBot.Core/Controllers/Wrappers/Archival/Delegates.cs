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
    XMessageModel current,
    XMessageModel? previous);
    
public delegate void UserDiffDelegate(
    UserChangeType type,
    XUserModel current,
    XUserModel? previous);