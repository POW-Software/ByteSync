using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class RemovePathItemRequest : IRequest<bool>
{
    public RemovePathItemRequest(string sessionId, Client client, EncryptedDataSource encryptedDataSource)
    {
        SessionId = sessionId;
        Client = client;
        EncryptedDataSource = encryptedDataSource;
    }
    
    public string SessionId { get; }
    
    public Client Client { get; }
    
    public EncryptedDataSource EncryptedDataSource { get; }
}