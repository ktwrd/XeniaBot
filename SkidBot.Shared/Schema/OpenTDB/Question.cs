using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkidBot.Shared.Schema.OpenTDB
{
    public class OpenTDBQuestion
    {
        [JsonPropertyName("category")]
        public string Category;
        [JsonPropertyName("type")]
        public string TypeValue;
        [JsonPropertyName("difficulty")]
        public string DifficultyValue;
        [JsonPropertyName("question")]
        public string Question;
        [JsonPropertyName("correct_answer")]
        public string CorrectAnswer;
        [JsonPropertyName("incorrect_answers")]
        public string[] IncorrectAnswers;

        [BsonIgnore]
        public OpenTDBDifficulty Difficulty
        {
            get
            {
                switch (DifficultyValue)
                {
                    case "easy":
                        return OpenTDBDifficulty.Easy;
                    case "medium":
                        return OpenTDBDifficulty.Medium;
                    case "hard":
                        return OpenTDBDifficulty.Hard;
                }
                return OpenTDBDifficulty.Unknown;
            }
        }

        [BsonIgnore]
        public OpenTDBType Type
        {
            get
            {
                switch (TypeValue)
                {
                    case "multiple":
                        return OpenTDBType.MultipleChoice;
                    case "boolean":
                        return OpenTDBType.Boolean;
                }
                return OpenTDBType.Unknown;
            }
        }

        [BsonIgnore]
        public string[] Answers
        {
            get
            {
                var rnd = new Random();
                var us = new string[]
                {
                    CorrectAnswer
                }.Concat(IncorrectAnswers).ToArray();
                rnd.Shuffle(us);
                return us;
            }
        }
    }
    public enum OpenTDBDifficulty
    {
        Unknown = -1,
        Easy,
        Medium,
        Hard
    }
    public enum OpenTDBType
    {
        Unknown = -1,
        MultipleChoice,
        Boolean
    }
}
