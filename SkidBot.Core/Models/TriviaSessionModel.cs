using kate.shared.Helpers;
using SkidBot.Shared.Schema.OpenTDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class TriviaSessionModel : BaseModel
    {
        public string SessionId = Guid.NewGuid().ToString();
        public ulong ChannelId;
        public ulong GuildId;
        public int MaxQuestions;
        public TriviaSessionQuestionModel[] QuestionStack = Array.Empty<TriviaSessionQuestionModel>();
        public int QuestionsCompleted = 0;
        public int CurrentQuestion = 0;
        /// <summary>
        /// Unix Timestamp when the current question timer started
        /// </summary>
        public long QuestionTimerStart = 0;
        /// <summary>
        /// Amount of time in seconds where the user must respond in time.
        /// </summary>
        public int QuestionAnswerTime = 10;
        public bool Complete = false;
        /// <summary>
        /// Did the users abort this session?
        /// </summary>
        public bool WasAborted = false;
        /// <summary>
        /// When was this session created?
        /// </summary>
        public long Timestamp;

        public TriviaSessionModel()
        { }
        public TriviaSessionModel(ulong guildId, ulong channelId, int questionCount)
        {
            GuildId = guildId;
            ChannelId = channelId;
            MaxQuestions = questionCount;
        }
    }
    public class TriviaSessionQuestionModel : OpenTDBQuestion
    {
        public List<TriviaSessionQuestionAnswerModel> UserAnswers = new List<TriviaSessionQuestionAnswerModel>();
        /// <summary>
        /// Unix timestamp when question was shown in session.
        /// </summary>
        public long QuestionTimestamp = 0;
        /// <summary>
        /// Snowflake of the message that members use to answer this question.
        /// </summary>
        public ulong MessageId;

        public static TriviaSessionQuestionModel FromQuestion(OpenTDBQuestion question)
        {
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true
            };
            var questionString = JsonSerializer.Serialize(question, options);
            var result = JsonSerializer.Deserialize<TriviaSessionQuestionModel>(questionString, options);
            return result;
        }
    }
    public class TriviaSessionQuestionAnswerModel
    {
        /// <summary>
        /// Discord User Snowflake
        /// </summary>
        public ulong UserId;
        public string Answer = "";
        /// <summary>
        /// True if user did not answer within the time limit
        /// </summary>
        public bool TooSlow = false;
        public long Timestamp = 0;
    }
}
