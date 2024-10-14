using System;
using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace XeniaBot.Shared;

public class StringPreviouslyNumericalSerializer : SerializerBase<string>
{
    public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.CurrentBsonType;
        switch (bsonType)
        {
            case BsonType.Null:
                context.Reader.ReadNull();
                return "0";
            case BsonType.String:
                return context.Reader.ReadString();
            case BsonType.Int32:
                return context.Reader.ReadInt32().ToString(CultureInfo.InvariantCulture);
            case BsonType.Int64:
                return context.Reader.ReadInt64().ToString(CultureInfo.InvariantCulture);
            default:
                var message = string.Format($"Custom Cannot deserialize BsonString, BsonInt32 or BsonInt64 from BsonType {bsonType}");
                throw new BsonSerializationException(message);
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            context.Writer.WriteString("0");
        }
        else
        {
            context.Writer.WriteString(value);
        }
    }
}