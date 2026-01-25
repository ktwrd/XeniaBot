using GenHTTP.Api.Protocol;

namespace XeniaDiscord.Shared.GenHttp;

public class CallbackActionItem(ActionPredicate predicate, ActionCallback callback) : BaseActionItem
{
    public override bool OnPredicate(IRequest request) => predicate.Invoke(request);
    public override Task<IResponse?> OnRequest(IRequest request) => callback.Invoke(request);
}

public delegate bool ActionPredicate(IRequest request);
public delegate Task<IResponse?> ActionCallback(IRequest request);
