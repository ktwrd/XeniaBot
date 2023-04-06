using kate.shared.Helpers;
using SkidBot.Shared.Schema.OpenTDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class TriviaSessionModel : BaseModel
    {
        public string SessionId;
        public ulong ChannelId;
        public ulong GuildId;
        public int MaxQuestions;
        public TriviaSessionQuestionModel[] QuestionStack;
        public int QuestionsCompleted;
        public bool Complete = false;
        public bool WasAborted = false;

        public TriviaSessionModel()
        {
            QuestionStack = Array.Empty<TriviaSessionQuestionModel>();
            QuestionsCompleted = 0;
            SessionId = Guid.NewGuid().ToString();
        }
    }
    public class TriviaSessionQuestionModel : OpenTDBQuestion
    {
        /// <summary>
        /// Key: Discord Snowflake
        /// Value: User Answer
        /// </summary>
        public Dictionary<ulong, string> UserAnswers = new Dictionary<ulong, string>();
    }
}
