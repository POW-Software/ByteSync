using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class RemovePathItemRequest : IRequest<bool>
{
    public RemovePathItemRequest(string sessionId, Client client, EncryptedPathItem encryptedPathItem)
    {
        SessionId = sessionId;
        Client = client;
        EncryptedPathItem = encryptedPathItem;
    }
    
    public string SessionId { get; }
    
    public Client Client { get; }
    
    public EncryptedPathItem EncryptedPathItem { get; }
}