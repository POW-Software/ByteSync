using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class StartInventoryRequest : IRequest<StartInventoryResult>
{
    public StartInventoryRequest(string sessionId, Client client)
    {
        SessionId = sessionId;
        Client = client;
    }
    
    public string SessionId { get; }
    
    public Client Client { get; }
}