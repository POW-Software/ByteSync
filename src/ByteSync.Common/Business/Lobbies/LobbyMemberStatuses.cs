namespace ByteSync.Common.Business.Lobbies;

public enum LobbyMemberStatuses
{
    WaitingForJoin = 1,
    Joined = 2,
    SecurityChecksSuccess = 3,
    TrustCheckError = 4,
    SecurityChecksInProgress = 5,
    CrossCheckError = 6,
    JoinedSession = 7,
    CreatedSession = 8,
    UnexpectedError = 9,
    Unknown_10 = 10,
}