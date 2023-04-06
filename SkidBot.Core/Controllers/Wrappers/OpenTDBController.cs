using SkidBot.Shared;
using SkidBot.Shared.Schema.OpenTDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkidBot.Core.Controllers.Wrappers
{
    [SkidController]
    public class OpenTDBController : BaseController
    {
        private HttpClient _httpClient;
        public OpenTDBController(IServiceProvider services)
            : base(services)
        {
            _httpClient = new HttpClient();
        }

        public const string Endpoint = "https://opentdb.com";
        public async Task<OpenTDBResponse> FetchQuestions(int amount = 10, string? category = null)
        {
            var url = $"{Endpoint}/api.php?amount={amount}";
            if (category != null)
            {
                url += $"&category={HttpUtility.UrlEncode(category)}";
            }
            var response = await _httpClient.GetAsync(url);
            var stringContent = response.Content.ReadAsStringAsync().Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                string errorContent = $"Failed to fetch questions from \"{url}\" (code: {response.StatusCode})\n========String Content========\n{stringContent}";
                Log.Error(errorContent);
                throw new Exception(errorContent);
                return null;
            }

            var deser = JsonSerializer.Deserialize<OpenTDBResponse>(stringContent, Program.SerializerOptions);
            if (deser == null)
            {
                throw new Exception("Failed to deserialize");
            }
            return deser;
        }
    }
}
