using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ByteSync.Common.Controls.Json;

public class TypeNameHandlingConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("La désérialisation avec noms de type n'est pas implémentée.");
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("$type", value.GetType().FullName); // Écrit le nom complet du type
        writer.WritePropertyName("data");
        JsonSerializer.Serialize(writer, value, value.GetType(), options); // Sérialise l'objet
        writer.WriteEndObject();
    }
}