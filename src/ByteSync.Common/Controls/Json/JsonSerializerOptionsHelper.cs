using System.Text.Json;
using System.Text.Json.Serialization;

namespace ByteSync.Common.Controls.Json;

public static class JsonSerializerOptionsHelper
{
    public static JsonSerializerOptions BuildOptions(bool writablePropertiesOnly, bool useUtcDateTimes, bool addTypeNames)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        if (writablePropertiesOnly)
        {
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        }

        if (useUtcDateTimes)
        {
            options.Converters.Add(new UtcDateTimeConverter());
        }

        if (addTypeNames)
        {
            options.Converters.Add(new TypeNameHandlingConverter());
        }
        
        options.ReferenceHandler = ReferenceHandler.Preserve;

        return options;
    }
}