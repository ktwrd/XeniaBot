using Microsoft.Extensions.DependencyInjection;
using Noppes.E621;
using SkidBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Controllers.Wrappers
{
    [SkidController]
    public class ESixController : BaseController
    {
        private IE621Client _client;
        private ConfigManager.Config _config;
        public ESixController(IServiceProvider services) : base (services)
        {
            _config = services.GetRequiredService<ConfigManager.Config>();

            _client = new E621ClientBuilder()
                .WithUserAgent("SkidBot", "1.0.0", "@kate@dariox.club", "Email")
                .Build();
            if (!_client.HasLogin)
            {
                Log.Error("Failed to login");
            }
        }

        public override async Task InitializeAsync()
        {
            try
            {
                await _client.LogInAsync(_config.ESix_Username, _config.ESix_ApiKey, false);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to login to e621\n{ex}");
            }
        }

        public async Task<Post[]> Query(string query, int? page=null)
        {
            var res = await _client.GetPostsAsync(query, page);
            return res.ToArray();
        }
    }
}
