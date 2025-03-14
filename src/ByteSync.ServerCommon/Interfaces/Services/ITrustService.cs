using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ITrustService
{
    Task SetAuthChecked(Client client, SetAuthCheckedParameters parameters);

    Task RequestTrustPublicKey(Client client, RequestTrustProcessParameters parameters);

    Task InformPublicKeyValidationIsFinished(Client client, PublicKeyValidationParameters parameters);
}