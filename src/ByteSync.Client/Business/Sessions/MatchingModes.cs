using System.Text.Json;
using System.Text.Json.Serialization;

namespace ByteSync.Business.Sessions;

[JsonConverter(typeof(MatchingModesJsonConverter))]
public enum MatchingModes
{
    Flat = 1,
    Tree = 2,
}

internal static class MatchingModeJsonHelper
{
    public static MatchingModes Read(ref Utf8JsonReader reader, string contextName)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (MatchingModeMapper.TryFromString(value, out var mode))
            {
                return mode;
            }
            
            throw new JsonException($"Unsupported {contextName} value: '{value}'");
        }
        
        if (reader.TokenType == JsonTokenType.Number)
        {
            var number = reader.GetInt32();
            
            return MatchingModeMapper.FromNumber(number);
        }
        
        throw new JsonException($"Unexpected token parsing {contextName}: {reader.TokenType}");
    }
}

internal static class MatchingModeMapper
{
    public static bool TryFromString(string? value, out MatchingModes mode)
    {
        if (value == null)
        {
            mode = default;
            
            return false;
        }
        
        if (value.Equals("Flat", StringComparison.OrdinalIgnoreCase) || value.Equals("Name", StringComparison.OrdinalIgnoreCase))
        {
            mode = MatchingModes.Flat;
            
            return true;
        }
        
        if (value.Equals("Tree", StringComparison.OrdinalIgnoreCase) || value.Equals("RelativePath", StringComparison.OrdinalIgnoreCase))
        {
            mode = MatchingModes.Tree;
            
            return true;
        }
        
        mode = default;
        
        return false;
    }
    
    public static MatchingModes FromNumber(int number) => number switch
    {
        1 => MatchingModes.Flat,
        2 => MatchingModes.Tree,
        _ => throw new JsonException($"Unsupported MatchingMode numeric value: {number}")
    };
    
    public static string ToNewName(MatchingModes value) => value switch
    {
        MatchingModes.Flat => "Flat",
        MatchingModes.Tree => "Tree",
        _ => value.ToString()
    };
    
    public static string ToLegacyName(MatchingModes value) => value switch
    {
        MatchingModes.Flat => "Name",
        MatchingModes.Tree => "RelativePath",
        _ => value.ToString()
    };
}

public class MatchingModesJsonConverter : JsonConverter<MatchingModes>
{
    public override MatchingModes Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return MatchingModeJsonHelper.Read(ref reader, "MatchingMode");
    }
    
    public override void Write(Utf8JsonWriter writer, MatchingModes value, JsonSerializerOptions options)
    {
        // Always emit new names
        writer.WriteStringValue(MatchingModeMapper.ToNewName(value));
    }
}

public class LegacyLinkingKeyJsonConverter : JsonConverter<MatchingModes>
{
    public override MatchingModes Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return MatchingModeJsonHelper.Read(ref reader, "LinkingKey");
    }
    
    public override void Write(Utf8JsonWriter writer, MatchingModes value, JsonSerializerOptions options)
    {
        // Emit legacy names so older clients keep working
        writer.WriteStringValue(MatchingModeMapper.ToLegacyName(value));
    }
}