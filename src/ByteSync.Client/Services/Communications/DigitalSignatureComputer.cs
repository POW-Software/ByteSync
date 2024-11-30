using System.Text;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Services.Communications;

namespace ByteSync.Services.Communications;

public class DigitalSignatureComputer : IDigitalSignatureComputer
{
    private readonly IConnectionService _connectionService;
    private readonly IPublicKeysManager _publicKeysManager;

    public DigitalSignatureComputer(IConnectionService connectionManager, IPublicKeysManager publicKeysManager)
    {
        _connectionService = connectionManager;
        _publicKeysManager = publicKeysManager;
    }
    
    public DigitalSignatureCheckInfo BuildDigitalSignatureCheckInfo(string sessionId, string recipientClientInstanceId,
        bool needsCrossCheck)
    {
        var myDigitalSignatureCheckInfo = new DigitalSignatureCheckInfo();
        myDigitalSignatureCheckInfo.DataId = sessionId;
        myDigitalSignatureCheckInfo.Issuer = _connectionService.ClientInstanceId!;
        myDigitalSignatureCheckInfo.NeedsCrossCheck = needsCrossCheck;
        myDigitalSignatureCheckInfo.Recipient = recipientClientInstanceId;

        var digitalSignature = ComputeMyDigitalSignature(sessionId, recipientClientInstanceId);
        var encodedSignature = _publicKeysManager.SignData(digitalSignature);
        myDigitalSignatureCheckInfo.Signature = encodedSignature;

        myDigitalSignatureCheckInfo.PublicKeyInfo = _publicKeysManager.GetMyPublicKeyInfo();

        return myDigitalSignatureCheckInfo;
    }

    public string ComputeOtherPartyExpectedSignature(string sessionId, string issuerClientInstanceId, PublicKeyInfo issuerPublicKeyInfo)
    {
        var result = DoComputeDigitalSignature(sessionId, issuerClientInstanceId, issuerPublicKeyInfo,
            _connectionService.ClientInstanceId!);

        return result;
    }
    
    private string ComputeMyDigitalSignature(string sessionId, string recipientClientInstanceId)
    {
        var result = DoComputeDigitalSignature(sessionId, _connectionService.ClientInstanceId!, _publicKeysManager.GetMyPublicKeyInfo(),
            recipientClientInstanceId);

        return result;
    }

    // https://crypto.stackexchange.com/questions/81929/how-exactly-is-signature-verification-done-in-ssh-v2-authentication
    // https://www.rfc-editor.org/rfc/rfc4252#page-9
    private string DoComputeDigitalSignature(string sessionId, string issuerClientInstanceId, PublicKeyInfo issuerPublicKeyInfo,
        string receiverClientInstanceId)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append("USERAUTH_REQUEST").Append('_');
        stringBuilder.Append(sessionId).Append('_');
        stringBuilder.Append(issuerClientInstanceId).Append('_');
        stringBuilder.Append(CryptographyUtils.ComputeSHA512(issuerPublicKeyInfo.PublicKey)).Append('_');
        stringBuilder.Append(receiverClientInstanceId);

        var digitalSignature = CryptographyUtils.ComputeSHA256FromText(stringBuilder.ToString());

        return digitalSignature;
    }
}