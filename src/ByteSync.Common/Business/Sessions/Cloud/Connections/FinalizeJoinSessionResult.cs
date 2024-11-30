using ByteSync.Common.Helpers;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public enum FinalizeJoinSessionStatuses
{
    FinalizeJoinSessionSucess = 1,
    SessionNotFound = 2,
    SessionAlreadyActivated = 3,
    PrememberNotFound = 4,
    AuthIsNotChecked = 5,
    
    Unknown_6 = 6,
    Unknown_7 = 7,
    Unknown_8 = 8,
    Unknown_9 = 9,
}

public class FinalizeJoinSessionResult
{
    public FinalizeJoinSessionStatuses Status { get; set; }
    
    // public CloudSessionResult CloudSessionResult { get; set; }

    public bool IsOK
    {
        get
        {
            return Status.In(FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess);
        }
    }

    public static FinalizeJoinSessionResult BuildFrom(FinalizeJoinSessionStatuses status)
    {
        FinalizeJoinSessionResult finalizeJoinSessionResult = new FinalizeJoinSessionResult();

        finalizeJoinSessionResult.Status = status;

        return finalizeJoinSessionResult;
    }

    // public static FinalizeJoinSessionResult BuildSuccess(CloudSessionResult cloudSessionResult)
    // {
    //     FinalizeJoinSessionResult joinSessionResult = new FinalizeJoinSessionResult();
    //
    //     joinSessionResult.Status = FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess;
    //     joinSessionResult.CloudSessionResult = cloudSessionResult;
    //
    //     return joinSessionResult;
    // }
}