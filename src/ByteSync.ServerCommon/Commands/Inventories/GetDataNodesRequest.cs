using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Security;
using ByteSync.Common.Business.Sessions;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class GetDataNodesRequest : IRequest<List<EncryptedDataNode>>, ISessionRequest
{
    public Client? Client { get; }

    public GetDataNodesRequest(string sessionId, string clientInstanceId, Client? client = null)
    {
        Client = client;
        SessionId = sessionId;
        ClientInstanceId = clientInstanceId;
    }
    
    public string SessionId { get; }
    
    public string ClientInstanceId { get; }
}