using ByteSync.Common.Controls.JSon;
using Newtonsoft.Json;

namespace ByteSync.Services.Misc;

public class JsonHelper
{
    public static string Serialize<T>(T data)
    {
        var settings = GetJsonSerializerSettings<T>();
        
        string json = JsonConvert.SerializeObject(data, Formatting.Indented, settings);

        return json;
    }

    public static T Deserialize<T>(string json)
    {
        var settings = GetJsonSerializerSettings<T>();

        var data = JsonConvert.DeserializeObject<T>(json, settings);

        return data;
    }

    private static JsonSerializerSettings GetJsonSerializerSettings<T>()
    {
        JsonSerializerSettings settings = JsonSerializerSettingsHelper.BuildSettings(true, true, true);

        return settings;
    }
}