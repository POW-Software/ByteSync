namespace ByteSync.Common.Business.Auth;

public enum InitialConnectionStatus
{
    Success = 1,
    VersionNotAllowed = 2,
    UnknownOsPlatform = 3,
    ClientAlreadyConnected = 4,
    UnknownError = 10,
}