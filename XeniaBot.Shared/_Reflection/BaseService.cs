using System;
using System.Threading.Tasks;

namespace XeniaBot.Shared;

public abstract class BaseService : IBaseService
{
    public int Priority { get; protected set; }
    protected IServiceProvider Services { get; }
    protected BaseService(IServiceProvider services)
    {
        Priority = Int32.MaxValue;
        Services = services;
    }

    /// <inheritdoc/>
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual Task OnReady()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual Task OnReadyDelay()
    {
        return Task.CompletedTask;
    }
}
public interface IBaseService : IXeniaOnInitialized, IXeniaOnReady, IXeniaOnReadyDelay
{
    public int Priority { get; }
}

public interface IXeniaOnInitialized
{
    /// <summary>
    /// Called once the Service Provider has been built, before logging into Discord.
    /// </summary>
    public Task InitializeAsync();
}
public interface IXeniaOnReady
{
    /// <summary>
    /// Called once the Discord Client is ready
    /// </summary>
    public Task OnReady();
}
public interface IXeniaOnReadyDelay
{
    /// <summary>
    /// Called 2s after all instances of <see cref="IXeniaOnReady"/> has been called, which themselves get called once Discord is ready.
    /// </summary>
    /// <returns></returns>
    public Task OnReadyDelay();
}