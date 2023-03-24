using SkidBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Controllers.Wrappers
{
    [SkidController]
    public class OpenTDBController : BaseController
    {
        private HttpClient httpClient;
        public OpenTDBController(IServiceProvider services)
            : base(services)
        {
            httpClient = new HttpClient();
        }
    }
}
