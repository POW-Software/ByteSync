using System.Text.Json.Serialization;

namespace ByteSync.Common.Business.Serials;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerialStatus
{
    OK = 1,
    // NotSupplied = 2,
    // NotFound = 3,
    Expired = 2,
    NoAvailableSlot = 3,
}