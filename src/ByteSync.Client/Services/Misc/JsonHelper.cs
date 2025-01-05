using System.Text.Json;
using ByteSync.Common.Controls.Json;

namespace ByteSync.Services.Misc;

public class JsonHelper
{
    public static string Serialize<T>(T data, bool includeTypeNames = true)
    {
        var options = GetJsonSerializerOptions<T>(includeTypeNames);
        
        string json = JsonSerializer.Serialize(data, options);

        return json;
    }

    public static T Deserialize<T>(string json, bool includeTypeNames = true)
    {
        var options = GetJsonSerializerOptions<T>(includeTypeNames);

        var data = JsonSerializer.Deserialize<T>(json, options);

        if (data == null)
        {
            throw new InvalidOperationException("Failed to deserialize JSON.");
        }

        return data;
    }


    private static JsonSerializerOptions GetJsonSerializerOptions<T>(bool includeTypeNames)
    {
        var options = JsonSerializerOptionsHelper.BuildOptions(true, true, includeTypeNames);

        return options;
    }
}