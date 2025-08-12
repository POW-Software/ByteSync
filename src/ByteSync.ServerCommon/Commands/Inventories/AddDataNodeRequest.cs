using ByteSync.ServerCommon.Business.Auth;
using ByteSync.Common.Business.Sessions;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class AddDataNodeRequest : IRequest<bool>
{
    public AddDataNodeRequest(string sessionId, Client client, string clientInstanceId, EncryptedDataNode encryptedDataNode)
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
