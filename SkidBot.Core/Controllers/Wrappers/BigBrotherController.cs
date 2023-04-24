using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using SkidBot.Shared;

namespace SkidBot.Core.Controllers.Wrappers;

[SkidController]
public class BigBrotherController : BaseController
{
    public BigBrotherController(IServiceProvider services)
        : base(services)
    {
        
    }
}
