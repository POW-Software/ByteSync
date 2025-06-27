using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class AddDataNodeRequest : IRequest<bool>
{
    public AddDataNodeRequest(string sessionId, Client client, string nodeId)
    {
        SessionId = sessionId;
        Client = client;
        NodeId = nodeId;
    }

    public string SessionId { get; }

    public Client Client { get; }

    public string NodeId { get; }
}
