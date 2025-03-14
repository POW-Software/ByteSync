using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ITrustService
{
    // Task<StartTrustCheckResult> StartTrustCheck(Client joiner, TrustCheckParameters trustCheckParameters);
    
    // Task SendDigitalSignatures(Client client, SendDigitalSignaturesParameters parameters);

    Task SetAuthChecked(Client client, SetAuthCheckedParameters parameters);

    Task RequestTrustPublicKey(Client client, RequestTrustProcessParameters parameters);

    // Task GiveMemberPublicKeyCheckData(Client client, GiveMemberPublicKeyCheckDataParameters parameters);

    Task InformPublicKeyValidationIsFinished(Client client, PublicKeyValidationParameters parameters);
}