using System.Text.Json;
using System.Text.Json.Serialization;

namespace ByteSync.Common.Controls.Json;

public static class JsonSerializerOptionsHelper
{
    public static JsonSerializerOptions BuildOptions(bool writablePropertiesOnly, bool useUtcDateTimes)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.Preserve,
            Converters = { new JsonStringEnumConverter() }
        };

        if (writablePropertiesOnly)
        {
            options.IgnoreReadOnlyProperties = true;
            options.IgnoreReadOnlyFields = true;
        }

        if (useUtcDateTimes)
        {
            options.Converters.Add(new UtcDateTimeConverter());
        }

        return options;
    }

    public static void SetOptions(JsonSerializerOptions options)
    {
        options.WriteIndented = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.ReferenceHandler = ReferenceHandler.Preserve;
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new UtcDateTimeConverter());
        
        options.IgnoreReadOnlyProperties = true;
        options.IgnoreReadOnlyFields = true;
    }
}