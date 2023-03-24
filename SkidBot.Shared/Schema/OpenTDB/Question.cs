using kate.shared.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkidBot.Shared.Schema.OpenTDB
{
    public class OpenTDBQuestion
    {
        [Browsable(false)]
        [JsonIgnore]
        public ObjectId _id { get; set; }

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

        [JsonIgnore]
        public string _QuestionHash => GetDataHash();
        public string GetDataHash()
        {
            var options = new JsonSerializerOptions()
            {
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                IncludeFields = true
            };
            var json = JsonSerializer.Serialize(this, options);
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(json));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        [BsonIgnore]
        [JsonIgnore]
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
        [JsonIgnore]
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
        [JsonIgnore]
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
