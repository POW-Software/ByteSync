using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class UpdateSessionSettingsRequest : IRequest
{
    public UpdateSessionSettingsRequest(string sessionId, Client client, EncryptedSessionSettings settings)
    {
        SessionId = sessionId;
        Client = client;
        Settings = settings;
    }

    public string SessionId { get; }
    
    public Client Client { get; }
    
    public EncryptedSessionSettings? Settings { get; }
}