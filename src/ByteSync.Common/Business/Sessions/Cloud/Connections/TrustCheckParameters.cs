using System.Collections.Generic;
using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class TrustCheckParameters
{
    public string SessionId { get; set; }
    
    public PublicKeyInfo PublicKeyInfo { get; set; }
    
    public List<string> MembersInstanceIdsToCheck { get; set; }
}