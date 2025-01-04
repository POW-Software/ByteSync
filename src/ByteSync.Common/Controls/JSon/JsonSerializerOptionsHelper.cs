using System.Text.Json;
using System.Text.Json.Serialization;

namespace ByteSync.Common.Controls.Json;

public static class JsonSerializerOptionsHelper
{
    public static JsonSerializerOptions BuildOptions(bool writablePropertiesOnly, bool useUtcDateTimes, bool addTypeNames)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true // Active ou désactive l'indentation pour le JSON formaté
        };

        if (writablePropertiesOnly)
        {
            // Ignore les propriétés ayant des valeurs par défaut
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        }

        if (useUtcDateTimes)
        {
            // Ajout d'un convertisseur personnalisé pour les DateTime en UTC
            options.Converters.Add(new UtcDateTimeConverter());
        }

        if (addTypeNames)
        {
            // Ajout d'un convertisseur personnalisé pour gérer les noms des types
            options.Converters.Add(new TypeNameHandlingConverter());
        }
        
        options.ReferenceHandler = ReferenceHandler.Preserve;

        return options;
    }
}