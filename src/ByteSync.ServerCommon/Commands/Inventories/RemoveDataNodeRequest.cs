using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class RemoveDataNodeRequest : IRequest<bool>
{
    public RemoveDataNodeRequest(string sessionId, Client client, string clientInstanceId, EncryptedDataNode encryptedDataNode)
    {
        SessionId = sessionId;
        Client = client;
        ClientInstanceId = clientInstanceId;
        EncryptedDataNode = encryptedDataNode;
    }

    public string SessionId { get; }

    public Client Client { get; }
    
    public string ClientInstanceId { get; }
    
    public EncryptedDataNode EncryptedDataNode { get; }
}
