using System;
using System.Threading.Tasks;

namespace SkidBot.Shared
{
    public abstract class BaseController
    {
        protected IServiceProvider _services;
        protected BaseController(IServiceProvider services)
        {
            _services = services;
        }
        public abstract Task InitializeAsync();
    }
}