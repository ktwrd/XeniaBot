using Microsoft.Extensions.DependencyInjection;

namespace XeniaDiscord.Data.Repositories;

public class BanSyncGuildRepository : IDisposable
{
    public void Dispose()
    {
        _serviceScope?.Dispose();
    }
    private readonly IServiceScope? _serviceScope;
    private readonly XeniaDbContext _db;
    public BanSyncGuildRepository(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out _serviceScope);
    }
}
