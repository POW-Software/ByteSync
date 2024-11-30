using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IDigitalSignatureComputer
{
    DigitalSignatureCheckInfo BuildDigitalSignatureCheckInfo(string sessionId, string recipientClientInstanceId, 
        bool needsCrossCheck);
    
    public string ComputeOtherPartyExpectedSignature(string sessionId, string issuerClientInstanceId, PublicKeyInfo issuerPublicKeyInfo);
}