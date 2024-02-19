using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;

namespace XeniaBot.Data.Services;

[XeniaController]
public class EvidenceDataService : BaseService
{
    private readonly ConfigData _configData;
    public EvidenceDataService(IServiceProvider services)
        : base(services)
    {
        _configData = services.GetRequiredService<ConfigData>();
    }
}