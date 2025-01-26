using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class StartInventoryRequest : IRequest<StartInventoryResult>
{
    public string SessionId { get; }
    public Client Client { get; }

    public StartInventoryRequest(string sessionId, Client client)
    {
        SessionId = sessionId;
        Client = client;
    }
}