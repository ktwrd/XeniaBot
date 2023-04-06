using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Controllers.BotAdditions;
using SkidBot.Core.Models;
using SkidBot.Shared;
using SkidBot.Shared.Schema.OpenTDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace SkidBot.Core.Controllers.Wrappers
{
    [SkidController]
    public class OpenTDBController : BaseController
    {
        private HttpClient _httpClient;
        private TriviaSessionController _session;
        public OpenTDBController(IServiceProvider services)
            : base(services)
        {
            _session = services.GetRequiredService<TriviaSessionController>();
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
            }

            var deser = JsonSerializer.Deserialize<OpenTDBResponse>(stringContent, Program.SerializerOptions);
            if (deser == null)
            {
                throw new Exception("Failed to deserialize");
            }
            return deser;
        }

        /// <summary>
        /// Safely create a trivia session.
        /// </summary>
        public async Task<TriviaSessionModel> CreateTriviaSession(ulong guildId, ulong channelId, int questionCount = 10, string? category = null)
        {
            if (questionCount < 1)
            {
                throw new ArgumentException($"Parameter questionCount must be greater than 0 (got {questionCount})");
            }

            var data = await _session.Get(guildId, channelId);
            if (data != null)
            {
                if (data.QuestionStack.Length <= data.QuestionsCompleted)
                {
                    throw new TriviaException("Session is already in progress");
                }
            }

            // Initialize new data and populate question stack.
            data = new TriviaSessionModel(guildId, channelId, questionCount);

            // Populate question stack
            OpenTDBResponse openTDBResponse;
            try
            {
                openTDBResponse = await FetchQuestions(questionCount, category);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to generate question stack.\n{ex}");
                throw new TriviaException("Failed to generate question stack.", ex);
            }

            // Validate response results
            if (openTDBResponse.Results.Length < 1 || openTDBResponse.Results.Length != questionCount)
            {
                Log.Error($"Invalid amount of questions recieved (Expected {questionCount}, got {openTDBResponse.Results.Length}");
                throw new TriviaException($"Invalid amount of questions recieved from server. Expected {questionCount}, got {openTDBResponse.Results.Length}");
            }

            // Cast OpenTDBQuestion to TriviaSessionQuestionModel
            var questionStackList = new List<TriviaSessionQuestionModel>();
            foreach (var i in openTDBResponse.Results)
            {
                questionStackList.Add(TriviaSessionQuestionModel.FromQuestion(i));
            }
            data.QuestionStack = questionStackList.ToArray();

            await _session.Set(data);

            // Should never be null since we just pushed it to the database above.
            return await _session.Get(data);
        }
    }
}
