using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface ITrustApiClient
{
    Task<StartTrustCheckResult?> StartTrustCheck(TrustCheckParameters parameters, CancellationToken cancellationToken = default);

    Task GiveMemberPublicKeyCheckData(GiveMemberPublicKeyCheckDataParameters parameters, CancellationToken cancellationToken = default);
    
    Task InformPublicKeyValidationIsFinished(PublicKeyValidationParameters parameters, CancellationToken cancellationToken = default);
    
    Task RequestTrustPublicKey(RequestTrustProcessParameters parameters, CancellationToken cancellationToken = default);
    
    Task SendDigitalSignatures(SendDigitalSignaturesParameters parameters, CancellationToken cancellationToken = default);
    
    Task SetAuthChecked(SetAuthCheckedParameters parameters, CancellationToken cancellationToken = default);
}