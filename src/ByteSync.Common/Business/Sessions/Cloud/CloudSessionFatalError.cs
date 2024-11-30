using System;

namespace ByteSync.Common.Business.Sessions.Cloud;

public enum CloudSessionFatalErrors
{
    MemberQuittedAfterActivation = 1,
    Unassigned2 = 2,
    Unassigned3 = 3,
    Unassigned4 = 4,
    Unassigned5 = 5,
}

public class CloudSessionFatalError
{
    public string SessionId { get; set; }
    
    public DateTimeOffset? HappenedOn { get; set; }
    
    public CloudSessionFatalErrors? SessionFatalError { get; set; }
}