using System.Collections.Generic;
using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class TrustCheckParameters
{
    public string SessionId { get; init; } = null!;
    
    public PublicKeyInfo PublicKeyInfo { get; init; } = null!;
    
    public List<string> MembersInstanceIdsToCheck { get; init; } = null!;
    
    public int ProtocolVersion { get; init; }
}