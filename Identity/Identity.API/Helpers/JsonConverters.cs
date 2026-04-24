using System.Text.Json;
using System.Text.Json.Serialization;

namespace Identity.API.Helpers;

public class StringOrNumberConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                return doc.RootElement.GetRawText();
            }
        }
        
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
