using GenHTTP.Api.Protocol;

namespace XeniaDiscord.Shared.GenHttp;

public class BaseActionItem
{
    public virtual bool OnPredicate(IRequest request)
    {
        throw new NotImplementedException();
    }
    public virtual Task<IResponse?> OnRequest(IRequest request)
    {
        throw new NotImplementedException();
    }
    public virtual Task PrepareAsync()
    {
        return Task.CompletedTask;
    }
}
