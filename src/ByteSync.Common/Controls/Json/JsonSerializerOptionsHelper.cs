using System.Text.Json;
using System.Text.Json.Serialization;

namespace ByteSync.Common.Controls.Json;

public static class JsonSerializerOptionsHelper
{
    public static JsonSerializerOptions BuildOptions()
    {
        var options = new JsonSerializerOptions();
        SetOptions(options);

        return options;
    }

    public static void SetOptions(JsonSerializerOptions options)
    {
        options.WriteIndented = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.ReferenceHandler = ReferenceHandler.Preserve;
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new UtcDateTimeConverter());
        
        options.PropertyNameCaseInsensitive = true;
        
        options.IgnoreReadOnlyProperties = true;
        options.IgnoreReadOnlyFields = true;
    }
}