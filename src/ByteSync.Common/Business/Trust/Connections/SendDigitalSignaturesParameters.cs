using System.Collections.Generic;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Common.Business.Trust.Connections;

public class SendDigitalSignaturesParameters
{
    public string DataId { get; set; } = null!;
    
    public List<DigitalSignatureCheckInfo> DigitalSignatureCheckInfos { get; set; } = null!;
    
    public bool IsAuthCheckOK { get; set; }
}