using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class RemoveDataSourceRequest : IRequest<bool>
{
    public RemoveDataSourceRequest(string sessionId, Client client, string nodeId, EncryptedDataSource encryptedDataSource)
    {
        SessionId = sessionId;
        Client = client;
        NodeId = nodeId;
        EncryptedDataSource = encryptedDataSource;
    }
    
    public string SessionId { get; }
    
    public Client Client { get; }

    public string NodeId { get; }
    
    public EncryptedDataSource EncryptedDataSource { get; }
}