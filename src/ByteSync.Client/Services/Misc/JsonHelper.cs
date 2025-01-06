using System.IO;
using System.Text.Json;
using ByteSync.Common.Controls.Json;

namespace ByteSync.Services.Misc;

public class JsonHelper
{
    public static string Serialize<T>(T data)
    {
        var options = GetJsonSerializerOptions<T>();
        
        string json = JsonSerializer.Serialize(data, options);

        return json;
    }

    public static T Deserialize<T>(string json)
    {
        var options = GetJsonSerializerOptions<T>();

        var data = JsonSerializer.Deserialize<T>(json, options);

        if (data == null)
        {
            throw new InvalidOperationException("Failed to deserialize JSON.");
        }

        return data;
    }
    
    public static T Deserialize<T>(Stream stream)
    {
        var options = GetJsonSerializerOptions<T>();

        var data = JsonSerializer.Deserialize<T>(stream, options);

        if (data == null)
        {
            throw new InvalidOperationException("Failed to deserialize JSON.");
        }

        return data;
    }
    
    private static JsonSerializerOptions GetJsonSerializerOptions<T>()
    {
        var options = JsonSerializerOptionsHelper.BuildOptions(true, true);

        return options;
    }
}