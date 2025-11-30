using ByteSync.Common.Helpers;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class JoinSessionResult
{
    public JoinSessionStatus Status { get; set; }
    
    public bool IsOK
    {
        get { return Status.In(JoinSessionStatus.SessionJoinedSuccessfully, JoinSessionStatus.ProcessingNormally); }
    }
    
    public static JoinSessionResult BuildFrom(JoinSessionStatus status)
    {
        JoinSessionResult joinSessionResult = new JoinSessionResult();
        
        joinSessionResult.Status = status;
        
        return joinSessionResult;
    }
    
    public static JoinSessionResult BuildProcessingNormally()
    {
        JoinSessionResult joinSessionResult = new JoinSessionResult();
        
        joinSessionResult.Status = JoinSessionStatus.ProcessingNormally;
        
        return joinSessionResult;
    }
}