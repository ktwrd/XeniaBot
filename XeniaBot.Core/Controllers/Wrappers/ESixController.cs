using Microsoft.Extensions.DependencyInjection;
using Noppes.E621;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace XeniaBot.Core.Controllers.Wrappers
{
    [BotController]
    public class ESixController : BaseController
    {
        private IE621Client _client;
        private ConfigData _configData;
        private string[] Ethanol = Array.Empty<string>();
        public ESixController(IServiceProvider services) : base (services)
        {
            _configData = services.GetRequiredService<ConfigData>();

            _client = new E621ClientBuilder()
                .WithUserAgent("XeniaBot", "1.0.0", "@kate@dariox.club", "Email")
                .Build();
            if (!_client.HasLogin)
            {
                Log.Error("Failed to login");
            }
        }

        private async Task UpdateEthanol()
        {
            string url =
                "https://gist.github.com/ktwrd/fc5380378cb92b6ffca48b9337310472/raw/3c1ae481d2e532a31b0c4c652ddbfa4d941ba3d7/ethanol.json";
            var client = new HttpClient();
            var response = await client.GetAsync(url);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Log.Error($"Failed to fetch tag blacklist. Error {response.StatusCode}");
                return;
            }

            var content = response.Content.ReadAsStringAsync().Result;
            var deser = JsonSerializer.Deserialize<string[]>(content) ?? Array.Empty<string>();
            Ethanol = deser;
        }

        public override async Task InitializeAsync()
        {
            if (_configData.ApiKeys.ESix.Enable == false)
            {
                Log.Warn($"ESix controller disabled");
                return;
            }
            try
            {
                await _client.LogInAsync(_configData.ApiKeys.ESix.Username, _configData.ApiKeys.ESix.ApiKey, false);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to login to e621\n{ex}");
            }

            await UpdateEthanol();
        }

        public async Task<Post[]> Query(string query, int? page=null)
        {
            if (_configData.ApiKeys.ESix.Enable == false)
                return Array.Empty<Post>();
            foreach (var i in Ethanol)
                query += $" -{i}";
            var res = await _client.GetPostsAsync(query, page);
            return res.ToArray();
        }
    }
}
