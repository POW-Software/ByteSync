using System;

namespace ByteSync.Common.Business.Sessions.Cloud;

public class UpdateSessionMemberGeneralStatusParameters
{
    public UpdateSessionMemberGeneralStatusParameters()
    {
        
    }
        
    public UpdateSessionMemberGeneralStatusParameters(string sessionId, string clientInstanceId, 
        SessionMemberGeneralStatus sessionMemberGeneralStatus, DateTimeOffset utcChangeDate)
    {
        SessionId = sessionId;
        ClientInstanceId = clientInstanceId;
        SessionMemberGeneralStatus = sessionMemberGeneralStatus;
        UtcChangeDate = utcChangeDate;
    }
    
    public string SessionId { get; set; }
    
    public string ClientInstanceId { get; set; }

    public SessionMemberGeneralStatus SessionMemberGeneralStatus { get; set; }
    
    public DateTimeOffset UtcChangeDate { get; set; }
}