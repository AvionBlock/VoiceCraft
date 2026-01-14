using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoiceCraft.Core.JsonConverters
{
    public class JsonBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.String => reader.GetString() switch
                {
                    "true" => true,
                    "false" => false,
                    _ => throw new JsonException()
                },
                JsonTokenType.None => throw new JsonException(),
                JsonTokenType.StartObject => throw new JsonException(),
                JsonTokenType.EndObject => throw new JsonException(),
                JsonTokenType.StartArray => throw new JsonException(),
                JsonTokenType.EndArray => throw new JsonException(),
                JsonTokenType.PropertyName => throw new JsonException(),
                JsonTokenType.Comment => throw new JsonException(),
                JsonTokenType.Number => throw new JsonException(),
                JsonTokenType.Null => throw new JsonException(),
                _ => throw new JsonException()
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }
}