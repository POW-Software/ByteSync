using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Security;

public interface ISessionRequest
{
    Client? Client { get; }
    string SessionId { get; }
}
