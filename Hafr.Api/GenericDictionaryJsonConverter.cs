using System.Text.Json;
using System.Text.Json.Serialization;

public class GenericDictionaryJsonConverter : JsonConverter<Dictionary<string, object?>>
{
    public override Dictionary<string, object?> Read(ref Utf8JsonReader reader, Type? typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Only objects are supported.");
        }

        var result = new Dictionary<string, object?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name.");
            }

            var propertyName = reader.GetString();

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new JsonException("Failed to get property name.");
            }

            if (!reader.Read())
            {
                throw new JsonException("Expected property value.");
            }

            result.Add(propertyName, ExtractValue(ref reader, options));
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object?> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }

    private object? ExtractValue(ref Utf8JsonReader reader, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.StartObject => Read(ref reader, null, options),
            JsonTokenType.StartArray => ReadArray(ref reader, options),
            JsonTokenType.String => ReadString(ref reader),
            JsonTokenType.Number => ReadNumber(ref reader),
            JsonTokenType.False => false,
            JsonTokenType.True => true,
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Token type '{reader.TokenType}' is not supported.")
        };

    private object ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var result = new List<object?>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            result.Add(ExtractValue(ref reader, options));
        }

        return result;
    }

    private static object? ReadString(ref Utf8JsonReader reader) =>
        reader.TryGetDateTimeOffset(out var dateTimeOffset)
            ? dateTimeOffset : reader.GetString();

    private static object ReadNumber(ref Utf8JsonReader reader) =>
        reader.TryGetInt64(out var result)
            ? result : reader.GetDecimal();
}
