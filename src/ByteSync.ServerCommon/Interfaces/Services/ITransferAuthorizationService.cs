using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ITransferAuthorizationService
{
    Task Authorize(string sessionId, Client client, TransferParameters parameters, bool upload, bool create);
}
