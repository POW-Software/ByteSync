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
            ReferenceHandler = ReferenceHandler.Preserve
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
}