using System.Collections.Generic;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class StartTrustCheckResult
{
    public StartTrustCheckResult()
    {
        MembersInstanceIds = new List<string>();
    }
    
    public bool IsOK { get; set; }
    
    public bool IsProtocolVersionIncompatible { get; set; }
    
    public List<string> MembersInstanceIds { get; set; }
}