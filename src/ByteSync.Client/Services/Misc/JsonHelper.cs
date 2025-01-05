using System.Text.Json;
using ByteSync.Common.Controls.Json;

namespace ByteSync.Services.Misc;

public class JsonHelper
{
    public static string Serialize<T>(T data, bool includeTypeNames = true)
    {
        var options = GetJsonSerializerOptions<T>(includeTypeNames);

        // Sérialisation en chaîne JSON avec options
        string json = JsonSerializer.Serialize(data, options);

        return json;
    }

    public static T Deserialize<T>(string json, bool includeTypeNames = true)
    {
        var options = GetJsonSerializerOptions<T>(includeTypeNames);

        // Désérialisation depuis une chaîne JSON avec options
        var data = JsonSerializer.Deserialize<T>(json, options);

        if (data == null)
        {
            throw new InvalidOperationException("Failed to deserialize JSON.");
        }

        return data;
    }


    private static JsonSerializerOptions GetJsonSerializerOptions<T>(bool includeTypeNames)
    {
        // Construire les options avec les paramètres personnalisés
        var options = JsonSerializerOptionsHelper.BuildOptions(true, true, includeTypeNames);

        return options;
    }
}