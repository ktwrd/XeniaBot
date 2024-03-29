using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace XeniaBot.Shared
{
    public abstract class BaseService
    {
        public int Priority { get; protected set; }
        protected IServiceProvider _services;
        protected BaseService(IServiceProvider services)
        {
            Priority = Int32.MaxValue;
            _services = services;
        }

        /// <summary>
        /// Called when all services have been added to the collection.
        /// </summary>
        /// <returns></returns>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when discord is ready
        /// </summary>
        /// <returns></returns>
        public virtual Task OnReady()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when discord is ready, but with a 2s delay.
        /// </summary>
        /// <returns></returns>
        public virtual Task OnReadyDelay()
        {
            return Task.CompletedTask;
        }
    }
}