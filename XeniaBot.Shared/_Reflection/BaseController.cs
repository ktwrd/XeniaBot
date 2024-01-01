using System;
using System.Threading.Tasks;

namespace XeniaBot.Shared
{
    public abstract class BaseController : IBaseController
    {
        protected IServiceProvider _services;
        protected BaseController(IServiceProvider services)
        {
            _services = services;
        }

        /// <inheritdoc />
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task OnReady()
        {
            return Task.CompletedTask;
        }
    }

    public interface IBaseController
    {
        /// <summary>
        /// Called when all services have been added to the collection.
        /// </summary>
        /// <returns></returns>
        public Task InitializeAsync();

        /// <summary>
        /// Called when discord is ready
        /// </summary>
        /// <returns></returns>
        public Task OnReady();
    }
}