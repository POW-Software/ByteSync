using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface ITrustApiClient
{
    Task<StartTrustCheckResult?> StartTrustCheck(TrustCheckParameters parameters);

    Task GiveMemberPublicKeyCheckData(GiveMemberPublicKeyCheckDataParameters parameters);
    
    Task InformPublicKeyValidationIsFinished(PublicKeyValidationParameters parameters);
    
    Task RequestTrustPublicKey(RequestTrustProcessParameters parameters);
    
    Task SendDigitalSignatures(SendDigitalSignaturesParameters parameters);
    
    Task SetAuthChecked(SetAuthCheckedParameters parameters);
}