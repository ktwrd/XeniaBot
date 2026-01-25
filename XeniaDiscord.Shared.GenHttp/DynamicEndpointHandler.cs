using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.IO;

namespace XeniaDiscord.Shared.GenHttp;

public class DynamicEndpointHandler(IReadOnlyList<BaseActionItem> actions) : IHandler
{
    private async Task InternalPrepareAsync()
    {
        foreach (var action in actions)
        {
            await action.PrepareAsync();
        }
    }

    public ValueTask PrepareAsync()
    {
        return new ValueTask(InternalPrepareAsync());
    }

    public ValueTask<IResponse?> HandleAsync(IRequest request)
    {
        var first = actions.FirstOrDefault(e => e.OnPredicate(request));
        if (first != null)
        {
            return new ValueTask<IResponse?>(first.OnRequest(request));
        }

        return new ValueTask<IResponse?>(request.Respond()
            .Status(ResponseStatus.NotFound)
            .Content("404: Not Found")
            .Type(new FlexibleContentType(ContentType.TextPlain))
            .Build());
    }
}
