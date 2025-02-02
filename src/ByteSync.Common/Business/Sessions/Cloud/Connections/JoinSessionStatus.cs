namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public enum JoinSessionStatus
{
    SessionJoinedSucessfully = 1,
    ProcessingNormally = 2,
    SessionNotFound = 3,
    ServerError = 4,
    TransientError = 5,
    TooManyMembers = 6,
    SessionAlreadyActivated = 7,
    TrustCheckFailed = 8,
    WrondPassword = 9,
    UnexpectedError = 10,
    Unknown_11 = 11,
    Unknown_12 = 12,
}