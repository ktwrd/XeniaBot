using GenHTTP.Api.Content;

namespace XeniaDiscord.Shared.GenHttp;

public class DynamicEndpointBuilder(IServiceProvider services) : IHandlerBuilder<DynamicEndpointBuilder>
{
    private readonly List<IConcernBuilder> _concerns = [];
    private readonly List<BaseActionItem> _actions = [];

    public DynamicEndpointBuilder Add(IConcernBuilder concern)
    {
        _concerns.Add(concern);
        return this;
    }
    public DynamicEndpointBuilder Add(BaseActionItem item)
    {
        _actions.Add(item);
        return this;
    }
    public DynamicEndpointBuilder AddHealth()
    {
        return Add(new HealthAction(services));
    }

    public IHandler Build()
    {
        return Concerns.Chain(_concerns, new DynamicEndpointHandler(_actions));
    }
}
