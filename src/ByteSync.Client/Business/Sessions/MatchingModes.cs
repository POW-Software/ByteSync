using System.Text.Json;
using System.Text.Json.Serialization;

namespace ByteSync.Business.Sessions;

[JsonConverter(typeof(MatchingModesJsonConverter))]
public enum MatchingModes
{
    Flat = 1,
    Tree = 2,
}

public class MatchingModesJsonConverter : JsonConverter<MatchingModes>
{
    public override MatchingModes Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.Equals(value, "Flat", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Name", StringComparison.OrdinalIgnoreCase))
            {
                return MatchingModes.Flat;
            }
            
            if (string.Equals(value, "Tree", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "RelativePath", StringComparison.OrdinalIgnoreCase))
            {
                return MatchingModes.Tree;
            }
            
            throw new JsonException($"Unsupported MatchingMode value: '{value}'");
        }
        
        if (reader.TokenType == JsonTokenType.Number)
        {
            var number = reader.GetInt32();
            
            return number switch
            {
                1 => MatchingModes.Flat,
                2 => MatchingModes.Tree,
                _ => throw new JsonException($"Unsupported MatchingMode numeric value: {number}")
            };
        }
        
        throw new JsonException($"Unexpected token parsing MatchingModes: {reader.TokenType}");
    }
    
    public override void Write(Utf8JsonWriter writer, MatchingModes value, JsonSerializerOptions options)
    {
        // Always emit new names
        writer.WriteStringValue(value switch
        {
            MatchingModes.Flat => "Flat",
            MatchingModes.Tree => "Tree",
            _ => value.ToString()
        });
    }
}