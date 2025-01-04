using System.Text.Json.Serialization;

namespace ByteSync.Common.Business.Auth;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InitialConnectionStatus
{
    Success = 1,
    VersionNotAllowed = 2,
    UnknownOsPlatform = 3,
    ClientAlreadyConnected = 4,
    UnknownError = 10,
}