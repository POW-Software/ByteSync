using ByteSync.Common.Helpers;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public enum JoinSessionStatuses
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

public class JoinSessionResult
{
    public JoinSessionStatuses Status { get; set; }

    public bool IsOK
    {
        get
        {
            return Status.In(JoinSessionStatuses.SessionJoinedSucessfully, JoinSessionStatuses.ProcessingNormally);
        }
    }

    public static JoinSessionResult BuildFrom(JoinSessionStatuses status)
    {
        JoinSessionResult joinSessionResult = new JoinSessionResult();

        joinSessionResult.Status = status;

        return joinSessionResult;
    }

    public static JoinSessionResult BuildProcessingNormally()
    {
        JoinSessionResult joinSessionResult = new JoinSessionResult();

        joinSessionResult.Status = JoinSessionStatuses.ProcessingNormally;

        return joinSessionResult;
    }
}