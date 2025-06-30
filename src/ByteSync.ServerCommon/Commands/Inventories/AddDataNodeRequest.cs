using ByteSync.ServerCommon.Business.Auth;
using ByteSync.Common.Business.Sessions;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class AddDataNodeRequest : IRequest<bool>
{
    public AddDataNodeRequest(string sessionId, Client client, EncryptedDataNode encryptedDataNode)
    {
        SessionId = sessionId;
        Client = client;
        EncryptedDataNode = encryptedDataNode;
    }

    public string SessionId { get; }

    public Client Client { get; }

    public EncryptedDataNode EncryptedDataNode { get; }
}
