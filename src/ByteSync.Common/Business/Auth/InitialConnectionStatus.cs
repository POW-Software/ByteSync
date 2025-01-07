namespace ByteSync.Common.Business.Auth;

public enum InitialConnectionStatus
{
    Success = 1,
    VersionNotAllowed = 2,
    UndefinedClientId = 3,
    UndefinedClientInstanceId = 4,
    UnknownOsPlatform = 5,
    ClientAlreadyConnected = 10,
    UnknownError = 11,
}