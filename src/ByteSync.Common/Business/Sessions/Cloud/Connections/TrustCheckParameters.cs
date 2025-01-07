using System.Collections.Generic;
using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class TrustCheckParameters
{
    public string SessionId { get; set; } = null!;

    public PublicKeyInfo PublicKeyInfo { get; set; } = null!;
    
    public List<string> MembersInstanceIdsToCheck { get; set; } = null!;
}