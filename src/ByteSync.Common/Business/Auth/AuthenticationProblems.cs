﻿namespace ByteSync.Common.Business.Auth;

public enum AuthenticationProblems
{
    EmailOrSerialNotFound = 1,
    SerialExpired = 2,
    RefreshTokenNotFound = 3,
    RefreshTokenNotActive = 4,
    ClientNotFound = 5,
    NoAvailableSlot = 6,
    VersionNotAllowed = 7,
    UnknownOsPlatform = 8,
}